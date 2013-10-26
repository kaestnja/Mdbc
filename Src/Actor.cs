﻿
/* Copyright 2011-2013 Roman Kuzmin
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Mdbc
{
	static class Actor
	{
		public const string ServerVariable = "Server";
		public const string DatabaseVariable = "Database";
		public const string CollectionVariable = "Collection";
		static bool _registered;
		public static void Register()
		{
			if (_registered)
				return;

			_registered = true;
			BsonSerializer.RegisterSerializer(typeof(Dictionary), new DictionarySerializer());
			BsonSerializer.RegisterSerializer(typeof(LazyDictionary), new LazyDictionarySerializer());
			BsonSerializer.RegisterSerializer(typeof(RawDictionary), new RawDictionarySerializer());
			BsonSerializer.RegisterSerializer(typeof(PSObject), new PSObjectSerializer());
		}
		public static object BaseObject(object value)
		{
			if (value == null)
				return value;
			var ps = value as PSObject;
			return ps == null ? value : ps.BaseObject;
		}
		public static object BaseObject(object value, out PSObject custom)
		{
			custom = null;
			if (value == null)
				return value;
			var ps = value as PSObject;
			if (ps == null)
				return value;
			if (!(ps.BaseObject is PSCustomObject))
				return ps.BaseObject;
			custom = ps;
			return ps;
		}
		public static object ToObject(BsonValue value) //_120509_173140 keep consistent
		{
			if (value == null)
				return null;

			switch (value.BsonType)
			{
				case BsonType.Array: return new Collection((BsonArray)value); // wrapper
				case BsonType.Binary: return BsonTypeMapper.MapToDotNetValue(value) ?? value; // byte[] or Guid else self
				case BsonType.Boolean: return ((BsonBoolean)value).Value;
				case BsonType.DateTime: return ((BsonDateTime)value).ToUniversalTime();
				case BsonType.Document: return new Dictionary((BsonDocument)value); // wrapper
				case BsonType.Double: return ((BsonDouble)value).Value;
				case BsonType.Int32: return ((BsonInt32)value).Value;
				case BsonType.Int64: return ((BsonInt64)value).Value;
				case BsonType.Null: return null;
				case BsonType.ObjectId: return ((BsonObjectId)value).Value;
				case BsonType.String: return ((BsonString)value).Value;
				default: return value;
			}
		}
		//! For external use only.
		public static BsonValue ToBsonValue(object value)
		{
			return ToBsonValue(value, null, 0);
		}
		static BsonValue ToBsonValue(object value, DocumentInput input, int depth)
		{
			IncSerializationDepth(ref depth);

			if (value == null)
				return BsonNull.Value;

			PSObject custom;
			value = BaseObject(value, out custom);

			// case: custom
			if (custom != null)
				return ToBsonDocumentFromProperties(null, custom, input, null, depth);

			// case: BsonValue
			var bson = value as BsonValue;
			if (bson != null)
				return bson;

			// case: string
			var text = value as string;
			if (text != null)
				return new BsonString(text);

			// case: dictionary
			var dictionary = value as IDictionary;
			if (dictionary != null)
				return ToBsonDocumentFromDictionary(null, dictionary, input, null, depth);

			// case: collection
			var enumerable = value as IEnumerable;
			if (enumerable != null)
			{
				var array = new BsonArray();
				foreach (var it in enumerable)
					array.Add(ToBsonValue(it, input, depth));
				return array;
			}

			// try to create BsonValue
			try
			{
				return BsonValue.Create(value);
			}
			catch (ArgumentException ae)
			{
				if (input == null)
					throw;

				try
				{
					value = input.ConvertValue(value);
				}
				catch (RuntimeException re)
				{
					throw new ArgumentException( //! use this type
						string.Format(null, @"Converter script was called on ""{0}"" and failed with ""{1}"".", ae.Message, re.Message), re);
				}

				if (value == null)
					throw;

				// do not call converter twice
				return ToBsonValue(value, null, depth);
			}
		}
		static BsonDocument ToBsonDocumentFromDictionary(BsonDocument source, IDictionary dictionary, DocumentInput input, IEnumerable<Selector> properties, int depth)
		{
			IncSerializationDepth(ref depth);

			//_131013_155413 reuse existing document as that is
			if (source == null && properties == null)
			{
				var md = dictionary as Dictionary;
				if (md != null)
					return md.Document();
			}

			// existing or new document
			var document = source ?? new BsonDocument();

			if (properties == null)
			{
				foreach (DictionaryEntry de in dictionary)
				{
					var name = de.Key as string;
					if (name == null)
						throw new InvalidOperationException("Dictionary keys must be strings.");

					document.Add(name, ToBsonValue(de.Value, input, depth));
				}
			}
			else
			{
				foreach (var selector in properties)
				{
					if (selector.PropertyName != null)
					{
						if (dictionary.Contains(selector.PropertyName))
							document.Add(selector.DocumentName, ToBsonValue(dictionary[selector.PropertyName], input, depth));
					}
					else
					{
						document.Add(selector.DocumentName, ToBsonValue(selector.GetValue(dictionary), input, depth));
					}
				}
			}

			return document;
		}
		// Input supposed to be not null
		static BsonDocument ToBsonDocumentFromProperties(BsonDocument source, PSObject value, DocumentInput input, IEnumerable<Selector> properties, int depth)
		{
			IncSerializationDepth(ref depth);

			// existing or new document
			var document = source ?? new BsonDocument();

			if (properties == null)
			{
				foreach (var pi in value.Properties)
				{
					try
					{
						document.Add(pi.Name, ToBsonValue(pi.Value, input, depth));
					}
					catch (GetValueException) // .Value may throw, e.g. ExitCode in Process
					{
						document.Add(pi.Name, BsonNull.Value);
					}
				}
			}
			else
			{
				foreach (var selector in properties)
				{
					if (selector.PropertyName != null)
					{
						var pi = value.Properties[selector.PropertyName];
						if (pi != null)
						{
							try
							{
								document.Add(selector.DocumentName, ToBsonValue(pi.Value, input, depth));
							}
							catch (GetValueException) // .Value may throw, e.g. ExitCode in Process
							{
								document.Add(selector.DocumentName, BsonNull.Value);
							}
						}
					}
					else
					{
						document.Add(selector.DocumentName, ToBsonValue(selector.GetValue(value), input, depth));
					}
				}
			}
			return document;
		}
		//! For external use only.
		public static BsonDocument ToBsonDocument(object value)
		{
			return ToBsonDocument(null, value, null, null, 0);
		}
		//! For external use only.
		public static BsonDocument ToBsonDocument(BsonDocument source, object value, DocumentInput input, IEnumerable<Selector> properties)
		{
			return ToBsonDocument(source, value, input, properties, 0);
		}
		static BsonDocument ToBsonDocument(BsonDocument source, object value, DocumentInput input, IEnumerable<Selector> properties, int depth)
		{
			IncSerializationDepth(ref depth);

			PSObject custom;
			value = BaseObject(value, out custom);

			//_131013_155413 reuse existing document as that is or wrap
			var document = value as BsonDocument;
			if (document != null)
			{
				// reuse existing
				if (source == null && properties == null)
					return document;

				// wrap
				value = new Dictionary(document);
			}

			var dictionary = value as IDictionary;
			if (dictionary != null)
				return ToBsonDocumentFromDictionary(source, dictionary, input, properties, depth);

			return ToBsonDocumentFromProperties(source, custom ?? new PSObject(value), input, properties, depth);
		}
		public static IEnumerable<BsonValue> ToEnumerableBsonValue(object value)
		{
			var bv = ToBsonValue(value, null, 0);
			var ba = bv as BsonArray;
			if (ba == null)
				return new[] { bv };
			else
				return ba;
		}
		public static IMongoQuery DocumentIdToQuery(BsonDocument document)
		{
			BsonValue id;
			if (!document.TryGetValue(MyValue.Id, out id))
				throw new ArgumentException("Document used as _id query must have _id."); //[1]

			return Query.EQ(MyValue.Id, id);
		}
		// for compilers
		public static BsonDocument ObjectToQueryDocument(object query)
		{
			if (query == null)
				return new BsonDocument();

			query = BaseObject(query);

			// unwrap bson wrapper
			var wrapper = query as BsonDocumentWrapper;
			if (wrapper != null)
				query = wrapper.WrappedObject;

			var builder = query as IMongoQuery;
			if (builder != null)
				return (BsonDocument)builder;

			BsonDocument document;
			if ((document = query as BsonDocument) != null)
				return document;

			throw new ArgumentException(string.Format(null, "Invalid query object type {0}.", query.GetType()));
		}
		public static IMongoQuery ObjectToQuery(object value)
		{
			if (value == null)
				return Query.Null;

			var ps = value as PSObject;
			if (ps != null)
			{
				value = ps.BaseObject;

				if (value is PSCustomObject)
				{
					var id = ps.Properties[MyValue.Id];
					if (id == null)
						throw new ArgumentException("Object used as _id query must have _id."); //[1]

					return Query.EQ(MyValue.Id, BsonValue.Create(id.Value));
				}
			}

			var query = value as IMongoQuery;
			if (query != null)
				return query;

			var mdbc = value as Dictionary;
			if (mdbc != null)
				return DocumentIdToQuery(mdbc.Document());

			var bson = value as BsonDocument;
			if (bson != null)
				return DocumentIdToQuery(bson);

			var dictionary = value as IDictionary;
			if (dictionary != null)
				return new QueryDocument(dictionary);

			return Query.EQ(MyValue.Id, BsonValue.Create(value));
		}
		/// <summary>
		/// Converts PS objects to a SortBy object.
		/// </summary>
		/// <param name="values">Strings or @{Name=Boolean}. Null and empty is allowed.</param>
		/// <returns>SortBy object, may be empty but not null.</returns>
		public static IMongoSortBy ObjectsToSortBy(IEnumerable values)
		{
			if (values == null)
				return SortBy.Null;

			var builder = new SortByBuilder();
			foreach (var it in values)
			{
				var name = it as string;
				if (name != null)
				{
					builder.Ascending(name);
					continue;
				}

				var hash = it as IDictionary;
				if (hash == null) throw new ArgumentException("SortBy: Invalid size object type.");
				if (hash.Count != 1) throw new ArgumentException("SortBy: Expected a dictionary with one entry.");

				foreach (DictionaryEntry kv in hash)
				{
					name = kv.Key.ToString();
					if (LanguagePrimitives.IsTrue(kv.Value))
						builder.Ascending(name);
					else
						builder.Descending(name);
				}
			}
			return builder;
		}
		public static IMongoFields ObjectsToFields(IList<object> values)
		{
			if (values == null)
				return null;

			IMongoFields fields;
			if (values.Count == 1 && (fields = values[0] as IMongoFields) != null)
				return fields;

			var builder = new FieldsBuilder();
			foreach (var it in values)
			{
				var name = it as string;
				if (name != null)
				{
					builder.Include(name);
					continue;
				}
				throw new ArgumentException("Property: Expected either one IMongoFields or one or more String.");
			}
			return builder;
		}
		public static IMongoUpdate ObjectToUpdate(object value, Action<string> error)
		{
			value = BaseObject(value);

			var update = value as IMongoUpdate;
			if (update != null)
				return update;

			var dictionary = value as IDictionary;
			if (dictionary != null)
				//_131102_084424 Do not pass IDictionary, it may have PSObject's
				return new UpdateDocument(ToBsonDocumentFromDictionary(null, dictionary, null, null, 0));

			var enumerable = LanguagePrimitives.GetEnumerable(value);
			if (enumerable != null)
				return Update.Combine(enumerable.Cast<object>().Select(x => ObjectToUpdate(x, error)));

			var message = string.Format(null, "Invalid update object type: {0}. Valid types: update(s), dictionary(s).", value.GetType());
			if (error == null)
				throw new ArgumentException(message);

			error(message);
			return null;
		}
		public static IEnumerable<BsonDocument> ObjectToBsonDocuments(object value)
		{
			var ps = value as PSObject;
			if (ps != null)
				value = ps.BaseObject;

			var r = new List<BsonDocument>();

			var enumerable = LanguagePrimitives.GetEnumerable(value);
			if (enumerable == null)
			{
				r.Add(ToBsonDocument(null, value, null, null, 0));
			}
			else
			{
				foreach (var it in enumerable)
					r.Add(ToBsonDocument(null, it, null, null, 0));
			}

			return r;
		}
		static void IncSerializationDepth(ref int depth)
		{
			if (++depth > BsonDefaults.MaxSerializationDepth)
				throw new InvalidOperationException("Data exceed the default maximum serialization depth.");
		}
	}
}

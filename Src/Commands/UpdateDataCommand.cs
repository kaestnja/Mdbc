﻿
// Copyright (c) Roman Kuzmin
// http://www.apache.org/licenses/LICENSE-2.0

using MongoDB.Bson;
using MongoDB.Driver;
using System.Management.Automation;

namespace Mdbc.Commands
{
	[Cmdlet(VerbsData.Update, "MdbcData"), OutputType(typeof(UpdateResult))]
	public sealed class UpdateDataCommand : AbstractCollectionCommand
	{
		//_131121_104038
		[Parameter(Position = 0)]
		public object Filter { get { return null; } set { _Filter = Actor.ObjectToFilter(value); } }
		FilterDefinition<BsonDocument> _Filter;

		//_131121_104038
		[Parameter(Position = 1)]
		public object Update { get { return null; } set { if (value != null) _Update = Api.UpdateDefinition(value); } }
		UpdateDefinition<BsonDocument> _Update;

		[Parameter]
		public SwitchParameter Add { get; set; }

		[Parameter]
		public SwitchParameter Many { get; set; }

		[Parameter]
		public UpdateOptions Options { get; set; }

		[Parameter]
		public SwitchParameter Result { get; set; }

		protected override void BeginProcessing()
		{
			if (_Filter == null) throw new PSArgumentException(Api.TextParameterFilter); //_131121_104038
			if (_Update == null) throw new PSArgumentException(Api.TextParameterUpdate);

			var options = Options ?? new UpdateOptions();
			if (Add)
				options.IsUpsert = true;

			try
			{
				UpdateResult result;
				if (Many)
				{
					result = Collection.UpdateMany(_Filter, _Update, options);
				}
				else
				{
					result = Collection.UpdateOne(_Filter, _Update, options);
				}

				if (Result)
					WriteObject(result);
			}
			catch (MongoException ex)
			{
				WriteException(ex, null);
			}
		}
	}
}

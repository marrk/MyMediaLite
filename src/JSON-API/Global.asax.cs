using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using MyMediaLite;
using MyMediaLite.Data;
using MyMediaLite.RatingPrediction;
using MyMediaLite.IO;
using System.Collections.Generic;

namespace JSONAPI
{
	[DataContract]
	[Description("MyMediaLite Web API.")]
	[RestService("/rating/{itemid}/{itemid}/{value}")] //Optional: Define an alternate REST-ful url for this service
	public class Rating
	{
		[DataMember]
		public string value {get;set;}
		[DataMember]
		public string itemid {get;set;}
		[DataMember]
		public string userid {get;set;}
	}

	[RestService("/recommendation/{userid}")] //Optional: Define an alternate REST-ful url for this service
	public class User
	{
		[DataMember]
		public string userid {get;set;}
	}

	public class Recommendation
	{
		public int ID { get; set; }
		public double prediction {get;set;}
		public IList<float> vector {get;set;}
	}
	public class StatusResponse
	{
		public string Result {get;set;}
	}

	public class MMAPIHost : AppHostBase
	{
		public MMAPIHost() 
			: base("MyMediaLite API", typeof(RecommenderService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			Recommender.Instance.init();
			container.Register(Recommender.Instance);

			Routes
				.Add<Rating>("/rating/{Userid}/{Itemid}/{Value}")
				.Add<StatusResponse>("/status")
				.Add<User>("/recommendation/{userid}");
		}
	}

	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new MMAPIHost().Init();
		}
	}
}



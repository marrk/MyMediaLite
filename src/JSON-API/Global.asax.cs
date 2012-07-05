using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using MyMediaLite;
using MyMediaLite.Data;
using MyMediaLite.RatingPrediction;
using MyMediaLite.IO;

namespace JSONAPI
{
	[DataContract]
	[Description("MyMediaLite Web API.")]
	[RestService("/hello")] //Optional: Define an alternate REST-ful url for this service
	[RestService("/hello/{User}")]
	[RestService("/hello/{User*}")] 
	public class Rating
	{
		[DataMember]
		public string Value {get;set;}
		[DataMember]
		public string Item {get;set;}
		[DataMember]
		public string User {get;set;}
	}

	public class Recommendation
	{
		public int ID { get; set; }
		public double prediction {get;set;}
		public double[] vector {get;set;}
	}
	public class StatusResponse
	{
		public string Result {get;set;}
	}

	public class RatingResponse
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
				.Add<StatusResponse>("/status");
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



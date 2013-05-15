using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;
using MyMediaLite;
using MyMediaLite.Data;
using MyMediaLite.RatingPrediction;
using MyMediaLite.IO;
using System.Collections.Generic;

namespace JSONAPI
{
	[DataContract]
	[Description("MyMediaLite Web API.")]
	[RestService("/rating/{userid}/{itemid}/{value}")] 
	public class Rating
	{
		[DataMember]
		public string value {get;set;}
		[DataMember]
		public string itemid {get;set;}
		[DataMember]
		public string userid {get;set;}
	}

	public class User
	{
		[DataMember]
		public string userid {get;set;}
		[DataMember]
		public string level {get;set;}
	}

    [RestService("/mu-668f8cbc-c9764138-f6fc077c-bc5b36b0")]
    public class Blitz {}

    public class BlitzService : IService<Blitz>
    {
        public object Execute(Blitz request)
        {
            return new HttpResult("42", "text");              
        }
    }

	public class Actor
	{
		public string name;
		public string role;
	}

	public class Movie
	{
		public string title;
		public string description;
		public string director;
		public string[] actors;
		public string[] roles;
	}

	public class Recommendation
	{
		public int ID { get; set; }
		public double prediction {get;set;}
		public IList<float> vector {get;set;}

		public Recommendation(int ID, double prediction, IList<float> vector){
			this.ID = ID;
			this.prediction = prediction;
			this.vector = vector;
		}

		public Recommendation ()
		{
		}
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
				.Add<User>("/recommendation/{userid}")	
				.Add<User>("/recommendation/{userid}/{level}")
				.Add<User>("/training/{userid}")
				.Add<User>("/training/{userid}/{level}");
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



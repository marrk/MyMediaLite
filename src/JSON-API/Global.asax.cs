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
	[Description("ServiceStack's Hello World web service.")]
	[RestService("/hello")] //Optional: Define an alternate REST-ful url for this service
	[RestService("/hello/{Name}")]
	[RestService("/hello/{Name*}")] 
	public class Hello
	{
		public string Name { get; set; }
	}

	public class HelloResponse
	{
		public string Result {get;set;}
	}

	public class MMAPIService : IService<Hello>
	{
		public object Execute(Hello request)
		{
			return new HelloResponse { Result = "Hello, " + request.Name };
		}
	}

	public class Global : System.Web.HttpApplication
	{
		public class MMAPIHost : AppHostBase
		{
			public MMAPIHost() : base("MyMediaLite API", typeof(MMAPIService).Assembly) {
				var user_mapping = new MyMediaLite.Data.EntityMapping(); 
				var item_mapping = new MyMediaLite.Data.EntityMapping();
				var recommender = new MyMediaLite.RatingPrediction.MatrixFactorization();
			}

			public override void Configure(Funq.Container Container)
			{
				Routes
					.Add<Hello>("/rating/{User}/{Item}/{Value}")
					.Add<Hello>("/hello/{Name}");
			}
		}
		protected void Application_Start(object sender, EventArgs e)
		{
			var mmapiHost = new MMAPIHost();
			mmapiHost.Init();
		}
	}
}



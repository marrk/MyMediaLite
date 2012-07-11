// Copyright (C) 2012 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Data;
using MyMediaLite;
using MyMediaLite.IO;
using MyMediaLite.RatingPrediction;
using MyMediaLite.Data;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.IO;

namespace JSONAPI
{
	public class Recommender
	{
		public TextWriter writer = new StreamWriter("addedratings.log");
		public static Recommender Instance = new Recommender();
		public EntityMapping user_mapping = new EntityMapping();
		public EntityMapping item_mapping = new EntityMapping();
		private static MatrixFactorization recommender;
		private static Timer _timer;
		private static MyMediaLite.Data.StackableRatings r = new MyMediaLite.Data.StackableRatings();

		private static Object _locker = new Object();
		static void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			StackableRatings stack;
			lock (_locker) {
				stack = r;
				r = new StackableRatings ();
			}
			_timer.Enabled = false;
			Console.WriteLine (DateTime.Now + " Adding " + stack.Count + " ratings");
			recommender.AddRatings (stack);
			Console.WriteLine (DateTime.Now + " Added " + stack.Count + " ratings");
			_timer.Enabled = true;
    	}

		public void init()
		{
			recommender = new MatrixFactorization();
			Console.WriteLine(DateTime.Now + " Started Reading Ratings");
			//recommender.LoadModel ("model.bin");
			//var training_data = RatingData.Read("tiny5Mratings.tsv", user_mapping, item_mapping);
			//recommender.Ratings = training_data;
			recommender.Ratings = new StackableRatings();
			Console.WriteLine(DateTime.Now + " Finished Reading Ratings");
			Console.WriteLine(DateTime.Now + " Started Training");
			recommender.Train();
			//recommender.SaveModel ("model.bin");
			Console.WriteLine(DateTime.Now + " Finished Training");
			_timer = new Timer(6000); // Set up the timer for 3 seconds
			_timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
			_timer.Enabled = true; // Enable it

		}
		
		public StatusResponse stats()
		{
			var items = recommender.Ratings.AllItems.Count;
			var users = recommender.Ratings.AllUsers.Count;
			var ratings = recommender.Ratings.Count;
			var allitems = recommender.Ratings.AllItems;
			var allusers = recommender.Ratings.AllUsers;
			TextWriter writer = new StreamWriter ("storedratings.log");
			foreach (var item in allitems) {
				foreach (var user in allusers) {
					float value;
					if(recommender.Ratings.TryGet (user, item, out value))
					{
						writer.WriteLine (user + " " + item + " " + value);
					}
				}
			}
			writer.Close ();
			item_mapping.SaveMapping ("itemmapping");
			user_mapping.SaveMapping ("usermapping");
			return new StatusResponse { Result = "Items: " + items + " Users: " + users + " Ratings: " + ratings};
	
		}
		public List<Recommendation> predict(int userid)
		{
			//recommender.RetrainUser (userid);
			//recommender.Train ();
			var allpredictions = recommender.ScoreItems(userid, recommender.Ratings.AllItems).OrderByDescending(prediction => prediction.Second).Take(20);
			//Recommendation recommendation = new Recommendation{ID = 2, prediction = 3.5, vector = new double[] {0.4, 0.5}};
			List<Recommendation> returnrecommendations = new List<Recommendation>();
			returnrecommendations.Add(new Recommendation(-1, -1, recommender.GetUserVector(userid)));

			foreach(MyMediaLite.DataType.Pair<int,float> prediction in allpredictions)
			{
				Recommendation newrecommendation = new Recommendation();
				newrecommendation.ID = Convert.ToInt32(item_mapping.ToOriginalID(prediction.First));
				newrecommendation.prediction = prediction.Second;
				newrecommendation.vector = recommender.GetItemVector(prediction.First);
				returnrecommendations.Add(newrecommendation);
			}
			return returnrecommendations;
		}

		/*
		public List<Recommendation> predict(int userid, Diversifier diversifier, float level)
		{
			//recommender.RetrainUser (userid);
			//recommender.Train ();
			var allpredictions = recommender.ScoreItems(userid, recommender.Ratings.AllItems);

			//Recommendation recommendation = new Recommendation{ID = 2, prediction = 3.5, vector = new double[] {0.4, 0.5}};
			List<Recommendation> returnrecommendations = new List<Recommendation>();
			returnrecommendations.Add(new Recommendation(-1, -1, recommender.GetUserVector(userid)));

			foreach(MyMediaLite.DataType.Pair<int,float> prediction in allpredictions)
			{
				Recommendation newrecommendation = new Recommendation();
				newrecommendation.ID = prediction.First;
				newrecommendation.prediction = prediction.Second;
				newrecommendation.vector = recommender.GetItemVector(prediction.First);
				returnrecommendations.Add(newrecommendation);
			}
			return returnrecommendations;
		}
		*/


		public StatusResponse AddRating(int userid, int itemid, float value)
		{
			//System.Random rnd = new System.Random();
			//userid = rnd.Next(1,200000);
			//itemid = rnd.Next(1,200000);
			//value = rnd.Next (0,4);
			r.AddAsync (userid, itemid, value, _locker);
			return new StatusResponse { Result = "OK"};

		}
	}

	public class RecommenderService : RestServiceBase<StatusResponse>
	{
		public Recommender recommender { get; set; }

		public override object OnGet(StatusResponse request)
		{
			return recommender.stats ();
		}
	}
	public class PredictionService : RestServiceBase<User>
	{
		public Recommender recommender { get; set; }

		public override object OnGet(User request)
		{
			int userid = Convert.ToInt32(recommender.user_mapping.ToInternalID(request.userid));
			if(request.level == ""){
				return recommender.predict (userid);
			} else {
				List<Recommendation> recommendations = recommender.predict (userid);
				//Diversify (recommendations, request.level);
				return recommendations;
			}
		}
	}

	public class RatingService : RestServiceBase<Rating>
	{	
		public Recommender recommender { get; set; }
		private static object _locker2 = new object();

		public override object OnGet(Rating request)
		{
			/*
			lock (_locker2) {
				recommender.writer.WriteLine (request.userid + " " + request.itemid + " " + request.value);
				recommender.writer.Flush ();
			}
			*/
			int userid = recommender.user_mapping.ToInternalID(request.userid);
			int itemid = recommender.item_mapping.ToInternalID(request.itemid);
			float value = Convert.ToSingle(request.value);
			return recommender.AddRating(userid, itemid, value);
		}
	}
}


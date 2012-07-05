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

namespace JSONAPI
{
	public class Recommender
	{
		public static Recommender Instance = new Recommender();
		private EntityMapping user_mapping;
		private EntityMapping item_mapping;
		private MatrixFactorization recommender;

		public void init()
		{
			recommender = new MatrixFactorization();
			var training_data = RatingData.Read("tiny5Mratings.tsv", user_mapping, item_mapping);
			recommender.Ratings = training_data;
			recommender.Train();
		}

		public StatusResponse stats()
		{
			var items = recommender.Ratings.AllItems.Count;
			var users = recommender.Ratings.AllUsers.Count;
			var ratings = recommender.Ratings.Count;
			return new StatusResponse { Result = "Items: " + items + " Users: " + users + " Ratings: " + ratings};
	
		}
		public List<Recommendation> predict(int userid)
		{
			//recommender.RetrainUser (userid);
			recommender.Train ();
			var allpredictions = recommender.ScoreItems(userid, recommender.Ratings.AllItems);
			//Recommendation recommendation = new Recommendation{ID = 2, prediction = 3.5, vector = new double[] {0.4, 0.5}};
			List<Recommendation> returnrecommendations = new List<Recommendation>();
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

		public StatusResponse AddRating(int userid, int itemid, float value)
		{
			System.Random rnd = new System.Random();
			userid = rnd.Next(1,2000000);
			itemid = rnd.Next(1,2000000);
			value = rnd.Next (0,4);
			recommender.Ratings.Add (userid, itemid, value);
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
			int userid = Convert.ToInt32 (request.userid);
			return recommender.predict (userid);
		}
	}

	public class RatingService : RestServiceBase<Rating>
	{	
		public Recommender recommender { get; set; }

		public override object OnGet(Rating request)
		{
			int userid = Convert.ToInt32(request.userid);
			int itemid = Convert.ToInt32(request.itemid);
			float value = Convert.ToSingle(request.value);
			return recommender.AddRating(userid, itemid, value);
		}
	}
}


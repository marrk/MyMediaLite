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
			var training_data = RatingData.Read("tiny5Mratings.csv");
			recommender.Ratings = training_data;
			recommender.Train();
		}

		public StatusResponse stats()
		{
			var items = recommender.Ratings.AllItems.Count;
			var users = recommender.Ratings.AllUsers.Count;

			return new StatusResponse { Result = "Items: " + items + " Users: " + users};
	
		}
		public Recommendation[] predict(int userid)
		{
			recommender.ScoreItems(userid, recommender.Ratings.AllItems);
			Recommendation[] returnrecommendations = new Recommendation[1];
			Recommendation recommendation = new Recommendation{ID = 2, prediction = 3.5, vector = new double[] {0.4, 0.5}};
			return returnrecommendations;
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
}


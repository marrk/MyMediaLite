// Copyright (C) 2010, 2011, 2012 Zeno Gantner
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

using System;
using System.Collections.Generic;
using System.Linq;
using MyMediaLite.Data;
using MyMediaLite.DataType;

namespace MyMediaLite.RatingPrediction
{
	/// <summary>Weighted user-based kNN</summary>
	public abstract class UserKNN : KNN, IUserSimilarityProvider, IFoldInRatingPredictor
	{
		/// <summary>boolean matrix indicating which user rated which item</summary>
		protected SparseBooleanMatrix data_user;

		///
		public override IRatings Ratings
		{
			set {
				base.Ratings = value;
				data_user = new SparseBooleanMatrix();
				for (int index = 0; index < ratings.Count; index++)
					data_user[ratings.Users[index], ratings.Items[index]] = true;
			}
		}

		/// <summary>Predict the rating of a given user for a given item</summary>
		/// <remarks>
		/// If the user or the item are not known to the recommender, a suitable average rating is returned.
		/// To avoid this behavior for unknown entities, use CanPredict() to check before.
		/// </remarks>
		/// <param name="user_id">the user ID</param>
		/// <param name="item_id">the item ID</param>
		/// <returns>the predicted rating</returns>
		public override float Predict(int user_id, int item_id)
		{
			if ((user_id > correlation.NumberOfRows - 1) || (item_id > MaxItemID))
				return baseline_predictor.Predict(user_id, item_id);

			IList<int> relevant_users = correlation.GetPositivelyCorrelatedEntities(user_id);

			double sum = 0;
			double weight_sum = 0;
			uint neighbors = K;
			foreach (int user_id2 in relevant_users)
			{
				if (data_user[user_id2, item_id])
				{
					float rating = ratings.Get(user_id2, item_id, ratings.ByItem[item_id]);

					float weight = correlation[user_id, user_id2];
					weight_sum += weight;
					sum += weight * (rating - baseline_predictor.Predict(user_id2, item_id));

					if (--neighbors == 0)
						break;
				}
			}

			float result = baseline_predictor.Predict(user_id, item_id);
			if (weight_sum != 0)
				result += (float) (sum / weight_sum);

			if (result > MaxRating)
				result = MaxRating;
			if (result < MinRating)
				result = MinRating;
			return result;
		}

		/// <summary>Retrain model for a given user</summary>
		/// <param name='user_id'>the user ID</param>
		abstract protected void RetrainUser(int user_id);

		///
		public override void AddRatings(IRatings ratings)
		{
			baseline_predictor.AddRatings(ratings);
			for (int index = 0; index < ratings.Count; index++)
				data_user[ratings.Users[index], ratings.Items[index]] = true;
			foreach (int user_id in ratings.AllUsers)
				RetrainUser(user_id);
		}

		///
		public override void UpdateRatings(IRatings ratings)
		{
			baseline_predictor.UpdateRatings(ratings);
			foreach (int user_id in ratings.AllUsers)
				RetrainUser(user_id);
		}

		///
		public override void RemoveRatings(IDataSet ratings)
		{
			baseline_predictor.RemoveRatings(ratings);
			for (int index = 0; index < ratings.Count; index++)
				data_user[ratings.Users[index], ratings.Items[index]] = true;
			foreach (int user_id in ratings.AllUsers)
				RetrainUser(user_id);
		}

		///
		protected override void AddUser(int user_id)
		{
			correlation.AddEntity(user_id);
		}

		///
		public float GetUserSimilarity(int user_id1, int user_id2)
		{
			return correlation[user_id1, user_id2];
		}

		///
		public IList<int> GetMostSimilarUsers(int user_id, uint n = 10)
		{
			return correlation.GetNearestNeighbors(user_id, n);
		}
		
		/// <summary>Fold in one user, identified by their ratings</summary>
		/// <returns>a vector containing the similarity with all users</returns>
		/// <param name='rated_items'>the ratings to take into account</param>
		abstract protected IList<float> FoldIn(IList<Pair<int, float>> rated_items);
		
		float Predict(IList<float> user_similarities, IList<Pair<int, float>> rated_items, int item_id)
		{
			if (item_id > MaxItemID)
				return baseline_predictor.Predict(int.MaxValue, item_id);

			IList<int> relevant_users = (
				from user_id in Enumerable.Range(0, user_similarities.Count)
				where user_similarities[user_id] > 0 
				select user_id).ToArray();
			
			/*
			var ratings = new Dictionary<int, float>();
			foreach (var pair in rated_items)
				ratings.Add(pair.First, pair.Second);
			*/
			
			double sum = 0;
			double weight_sum = 0;
			uint neighbors = K;
			foreach (int user_id in relevant_users)
			{
				if (data_user[user_id, item_id])
				{
					float rating = ratings.Get(user_id, item_id, ratings.ByUser[user_id]);

					float weight = user_similarities[user_id];
					weight_sum += weight;
					sum += weight * (rating - baseline_predictor.Predict(user_id, item_id));

					if (--neighbors == 0)
						break;
				}
			}

			float result = baseline_predictor.Predict(int.MaxValue, item_id); // TODO implement fold-in for baseline predictor
			if (weight_sum != 0)
				result += (float) (sum / weight_sum);

			if (result > MaxRating)
				result = MaxRating;
			if (result < MinRating)
				result = MinRating;
			return result;
		}
		
		///
		public IList<Pair<int, float>> ScoreItems(IList<Pair<int, float>> rated_items, IList<int> candidate_items)
		{
			var user_similarities = FoldIn(rated_items);

			// score the items
			var result = new Pair<int, float>[candidate_items.Count];
			for (int i = 0; i < candidate_items.Count; i++)
			{
				int item_id = candidate_items[i];
				result[i] = new Pair<int, float>(item_id, Predict(user_similarities, rated_items, item_id));
			}
			return result;
		}
	}
}
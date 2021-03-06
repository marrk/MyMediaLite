// Copyright (C) 2011, 2012 Zeno Gantner
// Copyright (C) 2010 Zeno Gantner, Steffen Rendle
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
using System.Globalization;
using System.Linq;
using MyMediaLite.Data;
using MyMediaLite.DataType;
using MyMediaLite.Eval.Measures;
using MyMediaLite.GroupRecommendation;

namespace MyMediaLite.Eval
{
	/// <summary>Evaluation class for group recommendation</summary>
	public static class Groups
	{
		/// <summary>Evaluation for rankings of items recommended to groups</summary>
		/// <remarks>
		/// </remarks>
		/// <param name="recommender">group recommender</param>
		/// <param name="test">test cases</param>
		/// <param name="train">training data</param>
		/// <param name="group_to_user">group to user relation</param>
		/// <param name="candidate_items">a collection of integers with all candidate items</param>
		/// <param name="ignore_overlap">if true, ignore items that appear for a group in the training set when evaluating for that user</param>
		/// <returns>a dictionary containing the evaluation results</returns>
		static public ItemRecommendationEvaluationResults Evaluate(
			this GroupRecommender recommender,
			IPosOnlyFeedback test,
			IPosOnlyFeedback train,
			SparseBooleanMatrix group_to_user,
			ICollection<int> candidate_items,
			bool ignore_overlap = true)
		{
			var result = new ItemRecommendationEvaluationResults();
			
			int num_groups = 0;
			
			foreach (int group_id in group_to_user.NonEmptyRowIDs)
			{
				var users = group_to_user.GetEntriesByRow(group_id);

				var correct_items = new HashSet<int>();
				foreach (int user_id in users)
					correct_items.UnionWith(test.UserMatrix[user_id]);
				correct_items.IntersectWith(candidate_items);

				var candidate_items_in_train = new HashSet<int>();
				foreach (int user_id in users)
					candidate_items_in_train.UnionWith(train.UserMatrix[user_id]);
				candidate_items_in_train.IntersectWith(candidate_items);
				int num_eval_items = candidate_items.Count - (ignore_overlap ? candidate_items_in_train.Count() : 0);

				// skip all groups that have 0 or #candidate_items test items
				if (correct_items.Count == 0)
					continue;
				if (num_eval_items - correct_items.Count == 0)
					continue;

				IList<int> prediction_list = recommender.RankItems(users, candidate_items);
				if (prediction_list.Count != candidate_items.Count)
					throw new Exception("Not all items have been ranked.");

				var ignore_items = ignore_overlap ? candidate_items_in_train : new HashSet<int>();

				double auc  = AUC.Compute(prediction_list, correct_items, ignore_items);
				double map  = PrecisionAndRecall.AP(prediction_list, correct_items, ignore_items);
				double ndcg = NDCG.Compute(prediction_list, correct_items, ignore_items);
				double rr   = ReciprocalRank.Compute(prediction_list, correct_items, ignore_items);
				var positions = new int[] { 5, 10 };
				var prec   = PrecisionAndRecall.PrecisionAt(prediction_list, correct_items, ignore_items, positions);
				var recall = PrecisionAndRecall.RecallAt(prediction_list, correct_items, ignore_items, positions);

				// thread-safe incrementing
				lock(result)
				{
					num_groups++;
					result["AUC"]       += (float) auc;
					result["MAP"]       += (float) map;
					result["NDCG"]      += (float) ndcg;
					result["MRR"]       += (float) rr;
					result["prec@5"]    += (float) prec[5];
					result["prec@10"]   += (float) prec[10];
					result["recall@5"]  += (float) recall[5];
					result["recall@10"] += (float) recall[10];
				}

				if (num_groups % 1000 == 0)
					Console.Error.Write(".");
				if (num_groups % 60000 == 0)
					Console.Error.WriteLine();
			}

			result["num_groups"] = num_groups;
			result["num_lists"]  = num_groups;
			result["num_items"]  = candidate_items.Count;

			return result;
		}
	}
}

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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MyMediaLite.Correlation;
using MyMediaLite.DataType;

namespace MyMediaLite.Diversification
{
	public class LatentFeatureDiversfication
	{
		EuclideanMatrix Distances { get; set; }

		/// <summary>Constructor</summary>
		/// <param name="item_correlation">the similarity measure to use for diversification</param>
		public LatentFeatureDiversfication(EuclideanMatrix item_correlation)
		{
			Distances = item_correlation;
		}

		public IList<int> DiversifySequential(IList<int> item_list, float diversification_parameter, int length)
		{
			Trace.Assert(item_list.Count > 0);
			Console.WriteLine ("Length " + length);
			var item_rank_by_rating = new Dictionary<int, int>();
			for (int i = 0; i < item_list.Count; i++)
				item_rank_by_rating[item_list[i]] = i;

			var diversified_item_list = new List<int>();
			int top_item = item_list[0];
			diversified_item_list.Add(top_item);

			var item_set = new HashSet<int>(item_list);
			item_set.Remove(top_item);

			while (item_set.Count > 0)
			{
				// rank remaining items by diversity
				var items_by_diversity = new List<Pair<int, float>>();
				foreach (int item_id in item_set)
				{
					float diversity = Diversity(item_id, diversified_item_list, Distances);
					items_by_diversity.Add(new Pair<int, float>(item_id, diversity));
				}
				items_by_diversity = items_by_diversity.OrderBy(x => x.Second).Reverse().ToList();

				var items_by_merged_rank = new List<Pair<int, float>>();
				for (int i = 0; i < items_by_diversity.Count; i++)
				{
					int item_id = items_by_diversity[i].First;
					// i is the dissimilarity rank
					// TODO adjust for ties
					float score = item_rank_by_rating[item_id] * (1f - diversification_parameter) + i * diversification_parameter;
					items_by_merged_rank.Add(new Pair<int, float>(item_id, score));
				}
				items_by_merged_rank = items_by_merged_rank.OrderBy(x => x.Second).ToList();

				int next_item_id = items_by_merged_rank[0].First;
				diversified_item_list.Add(next_item_id);
				item_set.Remove(next_item_id);
			}

			return diversified_item_list;
		}

		/// <summary>Compute similarity between one item and a collection of items</summary>
		/// <param name="item_id">the item ID</param>
		/// <param name="items">a collection of items</param>
		/// <param name="item_correlation">the similarity measure to use</param>
		/// <returns>the similarity between the item and the collection</returns>
		public static float Diversity(int item_id, ICollection<int> items, EuclideanMatrix distances)
		{
			double diversity  = 0;
			for (int i = 0; i < items.Count; i++)
				for (int j = i + 1; j < items.Count; j++)
					diversity += distances[i, j];
			return (float) diversity;
		}
	}
}


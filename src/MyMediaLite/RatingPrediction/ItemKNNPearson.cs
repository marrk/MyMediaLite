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

using System.Collections.Generic;
using MyMediaLite.Correlation;
using MyMediaLite.Taxonomy;
using MyMediaLite.Util;

namespace MyMediaLite.RatingPrediction
{
	/// <summary>Weighted item-based kNN with pearson correlation</summary>
	/// <remarks>
	/// This recommender supports incremental updates.
	/// </remarks>
	public class ItemKNNPearson : ItemKNN
	{
		/// <summary>shrinkage (regularization) parameter</summary>
		public float Shrinkage { get { return shrinkage; } set { shrinkage = value; } }
		private float shrinkage = 10;

		///
		public override void Train()
		{
			baseline_predictor.Train();
			this.correlation = Pearson.Create(ratings, EntityType.ITEM, Shrinkage);
			this.GetPositivelyCorrelatedEntities = Utils.Memoize<int, IList<int>>(correlation.GetPositivelyCorrelatedEntities);
		}

		///
		protected override void RetrainItem(int item_id)
		{
			baseline_predictor.RetrainItem(item_id);
			if (UpdateItems)
				for (int i = 0; i <= MaxItemID; i++)
					correlation[item_id, i] = Pearson.ComputeCorrelation(ratings, EntityType.ITEM, item_id, i, Shrinkage);
		}

		///
		public override string ToString()
		{
			return string.Format(
				"{0} k={1} shrinkage={2} reg_u={3} reg_i={4} num_iter={5}",
				this.GetType().Name, K == uint.MaxValue ? "inf" : K.ToString(), Shrinkage, RegU, RegI, NumIter);
		}
	}
}
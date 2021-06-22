/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using QuantConnect.Data;
using QuantConnect.DataSource;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Algorithm;

namespace QuantConnect.DataLibrary.Tests
{
    /// <summary>
    /// Example algorithm using the custom data type as a source of alpha
    /// </summary>
    public class QuiverEventsBetaDataAlgorithm : QCAlgorithm
    {
        private Symbol _customDataSymbol;
        private Symbol _equitySymbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            _equitySymbol = AddEquity("SPY").Symbol;
            _customDataSymbol = AddData<QuiverEventsBeta>(_equitySymbol).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            var data = slice.Get<QuiverEventsBeta>();
            if (!data.IsNullOrEmpty())
            {
                var betaEvent = data[_customDataSymbol];
                var isPresidentialElection2020 = betaEvent.EventName == "PresidentialElection2020";
                
                // The closer the election odds are, the higher we expect volatility to be, so we withdraw from the market.
                if (isPresidentialElection2020 && betaEvent.FirstEventOdds >= 0.45m && betaEvent.FirstEventOdds <= 0.55m)
                {
                    var holdingsPercentage = Math.Abs(betaEvent.FirstEventOdds - betaEvent.SecondEventOdds);
                    SetHoldings(_equitySymbol, holdingsPercentage);
                }
                // Less volatility expected, let's ease back in to the market according to the confidence of the election
                else if (isPresidentialElection2020)
                {
                    var holdingsPercentage = Math.Max(betaEvent.FirstEventOdds, betaEvent.SecondEventOdds);
                    SetHoldings(_equitySymbol, holdingsPercentage);
                }
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                Debug($"Purchased Stock: {orderEvent.Symbol}");
            }
        }
    }
}

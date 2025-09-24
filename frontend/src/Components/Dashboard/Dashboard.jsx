import React from "react";
import TopBar from "./Bars/TopBar";
import YearlySales from "./Bars/YearlySales";
import MonthlySales from "./Bars/MonthlySales";
import TopProducts from "./Bars/TopProducts";
import ProfitMarginProducts from "./Bars/ProfitMarginProducts";
import SalesByRegionAndCountry from "./Bars/SalesByRegionAndCountry";
import SalesAndCosts from "./Bars/SalesAndCosts";
import TopCustomers from "./Bars/TopCustomers";
import SalesByChannel from "./Bars/SalesByChannel";
import SalesVsPromotionsChart from "./Bars/SalesPromotions";

const Dashboard = ({ filters, showDetails }) => {

  const monthNames = [
    "",
    "January","February","March","April","May","June",
    "July","August","September","October","November","December"
  ];

  const monthFromName = filters.monthFrom ? monthNames[filters.monthFrom] : "";
  const monthToName = filters.monthTo ? monthNames[filters.monthTo] : "";

  return (
    <div className="dashboard container">
      <header>
        <div className="left">
          <h2>Overview</h2>
          <span>
            {showDetails && 'Details'}
          </span>
          <span>
            {filters.region}
          </span>
          <span>
            {filters.country}
          </span>
          <span>
            {filters.channel}
          </span>
          <span>
            {filters.year}
          </span>
          <span>{monthFromName}</span>
          <span>{filters.monthFrom && filters.monthTo && "-"}</span>
          <span>{monthToName}</span>
          <span>
            {filters.category}
          </span>
        </div>
        <div className="button">
          {/* <button>Generate report</button> */}
        </div>
      </header>
      <TopBar filters={filters} showDetails={showDetails}/>
      <div className="charts-grid two-columns-right">
        <YearlySales filters={filters} showDetails={showDetails}/>
        <MonthlySales filters={filters} showDetails={showDetails}/>
      </div>
      <div className="charts-grid three-columns">
        <TopProducts filters={filters} showDetails={showDetails}/>
        <ProfitMarginProducts filters={filters} showDetails={showDetails}/>
        <SalesAndCosts filters={filters} showDetails={showDetails}/>
      </div>
      <div className="charts-grid two-columns-left">
        <div className="left charts-grid">
          <div className="charts-grid">
            <SalesByRegionAndCountry filters={filters} showDetails={showDetails}/>
          </div>
          <div className="charts-grid two-columns">
            <TopCustomers filters={filters} showDetails={showDetails}/>
            <SalesByChannel filters={filters} showDetails={showDetails}/>
          </div>
        </div>
        <SalesVsPromotionsChart filters={filters} showDetails={showDetails}/>
      </div>
    </div>
  );
};

export default Dashboard;

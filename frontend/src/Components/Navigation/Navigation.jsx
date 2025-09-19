import React, { useEffect, useState } from 'react';
import "./Navigation.css";
import { getFilterData } from '../../Services/GetFilterData';

const Navigation = ({ filters, onFilterChange, showDetails, setShowDetails }) => {

  const [data, setData] = useState({
    regions: [],
    countries: [],
    channels: [],
    years: [],
    categories: []
  });

  const months = [
    { name: "January", index: 1 },
    { name: "February", index: 2 },
    { name: "March", index: 3 },
    { name: "April", index: 4 },
    { name: "May", index: 5 },
    { name: "June", index: 6 },
    { name: "July", index: 7 },
    { name: "August", index: 8 },
    { name: "September", index: 9 },
    { name: "October", index: 10 },
    { name: "November", index: 11 },
    { name: "December", index: 12 },
  ];

  const fetchData = async () => {
      try {
        console.log(filters.region)
        const fetchedData = await getFilterData(filters.region);
        setData(fetchedData);
      } catch (error) {
        console.log(error);
      }
  };

  useEffect(() => {
    fetchData();
  }, [filters.region]);

  const handleReset = () => {
    onFilterChange("region", ""); 
    onFilterChange("country", "");
    onFilterChange("channel", "");
    onFilterChange("year", "");
    onFilterChange("monthFrom", "");
    onFilterChange("monthTo", "");
    onFilterChange("category", "");
    onFilterChange("showDetails", false);
    fetchData("");
    setShowDetails(false);
  };

  return (
    <nav>
      <header>
        Sales History
        <p>Report</p>
      </header>
      <p>Dashboard</p>

      <form>
        <ul>
          <li>
            <div className="content">
              <label>Region</label>
              <select
                value={filters.region}
                onChange={e => onFilterChange("region", e.target.value)}
              >
                <option value=""></option>
                {data.regions.map(r => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
          </li>

          <li>
            <div className="content">
              <label>Country</label>
              <select
                value={filters.country}
                onChange={e => onFilterChange("country", e.target.value)}
              >
                <option value=""></option>
                {data.countries.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </li>

          <li>
            <div className="content">
              <label>Channel</label>
              <select
                value={filters.channel}
                onChange={e => onFilterChange("channel", e.target.value)}
              >
                <option value=""></option>
                {data.channels.map(ch => <option key={ch} value={ch}>{ch}</option>)}
              </select>
            </div>
          </li>

          <li>
            <div className="content">
              <label htmlFor="year">Year</label>
              <select
                  value={filters.year}
                  onChange={e => onFilterChange("year", e.target.value)}
                >
                  <option value=""></option>
                  {data.years.map(y => <option key={y} value={y}>{y}</option>)}
                </select>
            </div>
          </li>

          <li>
            <fieldset>
              <legend>Months</legend>
              <div>
                <span>From</span>
                <select
                  value={filters.monthFrom || ""}
                  onChange={e => onFilterChange("monthFrom", Number(e.target.value))}
                >
                  <option value=""></option>
                  {[
                    "January","February","March","April","May","June",
                    "July","August","September","October","November","December"
                  ].map((m, i) => (
                    <option key={i + 1} value={i + 1}>
                      {m}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <span>To</span>
                <select
                  value={filters.monthTo || ""}
                  onChange={e => onFilterChange("monthTo", Number(e.target.value))}
                >
                  <option value=""></option>
                  {[
                    "January","February","March","April","May","June",
                    "July","August","September","October","November","December"
                  ].map((m, i) => (
                    <option key={i + 1} value={i + 1}>
                      {m}
                    </option>
                  ))}
                </select>
              </div>
            </fieldset>
          </li>


          <li>
            <div className="content">
              <label>Category</label>
              <select
                value={filters.category}
                onChange={e => onFilterChange("category", e.target.value)}
              >
                <option value=""></option>
                {data.categories.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </li>

          <li>
            <div className="content" onClick={()=>setShowDetails(prev => !prev)}>
              <label>Show details</label>
              <input
                type="checkbox"
                checked={showDetails}
              />
            </div>
          </li>
        </ul>

        <div className="buttons">
          <button type='reset'  onClick={handleReset}>Reset</button>
        </div>
      </form>
    </nav>
  );
};

export default Navigation;

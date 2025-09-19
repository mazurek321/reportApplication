import React, { useState } from "react";
import Navigation from "./Components/Navigation/Navigation";
import Dashboard from "./Components/Dashboard/Dashboard";

function App() {
  const [filters, setFilters] = useState({
    region: "",
    country: "",
    channel: "",
    year: "",
    monthFrom: "",
    monthTo: "",
    category: ""
  });

  const [showDetails, setShowDetails] = useState(false);

  const handleFilterChange = (name, value) => {
    setFilters(prev => ({
      ...prev,
      [name]: value
    }));
  };

  return (
    <>
      <Navigation onFilterChange={handleFilterChange} filters={filters} showDetails={showDetails} setShowDetails={setShowDetails}/>
      <Dashboard filters={filters} showDetails={showDetails}/>
    </>
  );
}

export default App;

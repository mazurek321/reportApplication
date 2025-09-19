import axios from "axios";

const API_URL = "http://localhost:5059/api/FilterOptions";

export const getFilterData = async (region) => {
  const response = await axios.get(`${API_URL}${region ? `?region=${region}` : ""}`);
  return response.data;
};

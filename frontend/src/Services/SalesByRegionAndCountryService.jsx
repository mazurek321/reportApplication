import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/sales-by-region-country";

export const getSalesByRegionAndCountry = (filters) =>
  apiGet(API_URL, filters);

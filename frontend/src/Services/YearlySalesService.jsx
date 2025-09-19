import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/yearly-sales";

export const getYearlySalesData = (filters) =>
  apiGet(API_URL, filters);

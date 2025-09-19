import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/top-data";

export const getTopBarData = (filters) =>
  apiGet(API_URL, filters);

import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/sales-costs";

export const getSalesAndCostsData = (filters) =>
  apiGet(API_URL, filters);

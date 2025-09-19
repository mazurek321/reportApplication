import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/monthly-sales-with-previous";

export const gerMonthlySalesComparisonWithPreviousYear = (filters) =>
  apiGet(API_URL, filters);
import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/top-products-profit";

export const getProfitMarginProducts = (filters) =>
  apiGet(API_URL, filters);

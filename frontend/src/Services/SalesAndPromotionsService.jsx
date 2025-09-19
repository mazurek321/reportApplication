import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/sales-vs-promotions";

export const getSalesAndPromotionsData = (filters) =>
  apiGet(API_URL, filters);

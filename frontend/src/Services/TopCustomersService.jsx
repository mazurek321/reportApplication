import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/top-customers";

export const getTopCustomersData = (filters) =>
  apiGet(API_URL, filters);

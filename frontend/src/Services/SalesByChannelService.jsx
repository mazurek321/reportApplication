import { apiGet } from "./ApiHelper";

const API_URL = "http://localhost:5059/api/Report/sales-by-channel";

export const getSalesByChannelData = (filters) =>
  apiGet(API_URL, filters);

import axios from "axios";

export const apiGet = async (url, filters = {}) => {
  const response = await axios.get(url, { params: filters });
  console.log(url, {params: filters})
  return response.data;
};
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: __BASE_URL__,
});

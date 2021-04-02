/* tslint:disable */

/**
 * This file was automatically generated by "Swaxios".
 * It should not be modified by hand.
 */

import {AxiosInstance, AxiosRequestConfig} from 'axios';
import {UserApplication} from '../../interfaces/';

export class ApplicationsService {
  private readonly apiClient: AxiosInstance;

  constructor(apiClient: AxiosInstance) {
    this.apiClient = apiClient;
  }

  getAll = async (): Promise<Array<UserApplication>> => {
    const config: AxiosRequestConfig = {
      method: 'get',
      url: '/v1.0/applications',
    };
    const response = await this.apiClient.request<Array<UserApplication>>(
      config
    );
    return response.data;
  };
}
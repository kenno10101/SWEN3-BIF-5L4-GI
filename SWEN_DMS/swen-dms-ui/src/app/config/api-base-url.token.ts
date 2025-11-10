import { InjectionToken } from '@angular/core';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    if (location.hostname === 'localhost') {
      return 'http://localhost:8080/api';
    }
    return '/api';
  },
});

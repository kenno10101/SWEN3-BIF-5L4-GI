import { InjectionToken } from '@angular/core';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    // Dev: ng serve (4200) -> REST (8080)
    if (location.hostname === 'localhost' && location.port === '4200') {
      return 'http://localhost:8080/api';
    }
    // Docker: UI (8081) -> nginx -> /api -> rest-server
    return '/api';
  },
});


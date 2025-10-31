import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { DashboardComponent } from './app/pages/dashboard/dashboard.component';

bootstrapApplication(DashboardComponent, {
  providers: [
    provideHttpClient(),
    provideRouter(routes),
  ],
}).catch(err => console.error(err));

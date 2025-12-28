import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { 
  API_BASE_URL, 
  HealthClient, 
  AdminClient, 
  AdminIngredientGroupClient, 
  AdminRecipeClient, 
  AdminRecipeSourceClient, 
  AdminReferenceClient, 
  ScrapeClient, 
  AdminUnmappedIngredientClient, 
  ReferenceClient, 
  SearchClient, 
  SubstitutionClient, 
  UserIngredientsClient, 
  DbClient, 
  AuthClient 
} from './services/pantry-raid-api';
import { environment } from '../environments/environment';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
    
    // API Clients
    HealthClient,
    AdminClient,
    AdminIngredientGroupClient,
    AdminRecipeClient,
    AdminRecipeSourceClient,
    AdminReferenceClient,
    ScrapeClient,
    AdminUnmappedIngredientClient,
    ReferenceClient,
    SearchClient,
    SubstitutionClient,
    UserIngredientsClient,
    DbClient,
    AuthClient
  ]
};

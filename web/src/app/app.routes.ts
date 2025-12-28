import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home';
import { AboutComponent } from './components/about/about';
import { AccountComponent } from './components/account/account';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { UnmappedIngredientsComponent } from './components/admin/unmapped-ingredients/unmapped-ingredients';
import { SubstitutionsComponent } from './components/admin/substitutions/substitutions';
import { ScrapingComponent } from './components/admin/scraping/scraping';
import { IngredientGroupsComponent } from './components/admin/ingredient-groups/ingredient-groups';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'about', component: AboutComponent },
  { path: 'account', component: AccountComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: 'admin',
    children: [
      { path: 'unmapped-ingredients', component: UnmappedIngredientsComponent },
      { path: 'substitutions', component: SubstitutionsComponent },
      { path: 'scraping', component: ScrapingComponent },
      { path: 'ingredient-groups', component: IngredientGroupsComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];

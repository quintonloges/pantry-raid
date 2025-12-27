import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { ReferenceClient, IngredientDto, IngredientGroupDto } from './pantry-raid-api';

const STORAGE_KEY = 'pantry-raid-ingredients';

@Injectable({
  providedIn: 'root'
})
export class IngredientService {
  private selectedIngredientsSubject = new BehaviorSubject<IngredientDto[]>([]);
  public selectedIngredients$ = this.selectedIngredientsSubject.asObservable();

  constructor(private referenceClient: ReferenceClient) {
    this.loadFromStorage();
  }

  private loadFromStorage(): void {
    const stored: string | null = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      try {
        const ingredients: IngredientDto[] = JSON.parse(stored).map((i: any) => IngredientDto.fromJS(i));
        this.selectedIngredientsSubject.next(ingredients);
      } catch (e) {
        console.error('Failed to load ingredients from storage', e);
        this.selectedIngredientsSubject.next([]);
      }
    }
  }

  private saveToStorage(ingredients: IngredientDto[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(ingredients));
  }

  getGroups(): Observable<IngredientGroupDto[]> {
    return this.referenceClient.getIngredientGroups();
  }

  searchIngredients(query: string): Observable<IngredientDto[]> {
    if (!query || query.length < 2) {
      return of([]);
    }
    return this.referenceClient.getIngredients(query);
  }

  addIngredient(ingredient: IngredientDto): void {
    const current: IngredientDto[] = this.selectedIngredientsSubject.value;
    if (!current.find(i => i.id === ingredient.id)) {
      const updated: IngredientDto[] = [...current, ingredient].sort((a, b) => (a.name || '').localeCompare(b.name || ''));
      this.selectedIngredientsSubject.next(updated);
      this.saveToStorage(updated);
    }
  }

  addIngredients(ingredients: IngredientDto[]): void {
    const current: IngredientDto[] = this.selectedIngredientsSubject.value;
    const newIngredients: IngredientDto[] = ingredients.filter(i => !current.find(existing => existing.id === i.id));
    
    if (newIngredients.length > 0) {
      const updated: IngredientDto[] = [...current, ...newIngredients].sort((a, b) => (a.name || '').localeCompare(b.name || ''));
      this.selectedIngredientsSubject.next(updated);
      this.saveToStorage(updated);
    }
  }

  removeIngredient(id: number): void {
    const current: IngredientDto[] = this.selectedIngredientsSubject.value;
    const updated: IngredientDto[] = current.filter(i => i.id !== id);
    this.selectedIngredientsSubject.next(updated);
    this.saveToStorage(updated);
  }

  clearIngredients(): void {
    this.selectedIngredientsSubject.next([]);
    this.saveToStorage([]);
  }
}


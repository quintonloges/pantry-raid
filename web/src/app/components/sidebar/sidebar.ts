import { Component, OnInit, ElementRef, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteSelectedEvent, MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Observable, of } from 'rxjs';
import { debounceTime, switchMap, startWith } from 'rxjs/operators';
import { IngredientService } from '../../services/ingredient.service';
import { IngredientDto, IngredientGroupDto } from '../../services/pantry-raid-api';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatChipsModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule
  ],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class SidebarComponent implements OnInit {
  ingredientService: IngredientService = inject(IngredientService);
  
  ingredientCtrl: FormControl<string | null> = new FormControl('');
  filteredIngredients: Observable<IngredientDto[]>;
  ingredientGroups$: Observable<IngredientGroupDto[]>;
  
  selectedIngredients$: Observable<IngredientDto[]> = this.ingredientService.selectedIngredients$;

  @ViewChild('ingredientInput') ingredientInput!: ElementRef<HTMLInputElement>;

  constructor() {
    this.filteredIngredients = this.ingredientCtrl.valueChanges.pipe(
      startWith(null),
      debounceTime(300),
      switchMap((value: string | null): Observable<IngredientDto[]> => {
        if (typeof value === 'string' && value.length >= 2) {
          return this.ingredientService.searchIngredients(value);
        } else {
          return of([]);
        }
      })
    );
    
    this.ingredientGroups$ = this.ingredientService.getGroups();
  }

  ngOnInit(): void {}

  selected(event: MatAutocompleteSelectedEvent): void {
    const ingredient: IngredientDto = event.option.value as IngredientDto;
    this.ingredientService.addIngredient(ingredient);
    this.ingredientInput.nativeElement.value = '';
    this.ingredientCtrl.setValue(null);
  }

  remove(ingredient: IngredientDto): void {
    if (ingredient.id) {
      this.ingredientService.removeIngredient(ingredient.id);
    }
  }

  addGroup(group: IngredientGroupDto): void {
    if (group.items) {
      const ingredients: IngredientDto[] = group.items.map(item => new IngredientDto({
        id: item.ingredientId,
        name: item.name
      }));
      this.ingredientService.addIngredients(ingredients);
    }
  }

  clearAll(): void {
    this.ingredientService.clearIngredients();
  }
}


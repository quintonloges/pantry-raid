import { Component, OnInit, inject, ViewChild, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SidebarComponent } from '../sidebar/sidebar';
import { ResultsComponent } from '../results/results';
import { IngredientService } from '../../services/ingredient.service';
import { SearchService } from '../../services/search.service';
import { SearchResponseDto, SearchRequestDto, IngredientDto } from '../../services/pantry-raid-api';
import { Subject, takeUntil, Observable, of } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule, 
    MatSidenavModule, 
    MatButtonModule, 
    MatIconModule,
    SidebarComponent, 
    ResultsComponent
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class HomeComponent implements OnInit, OnDestroy {
  ingredientService: IngredientService = inject(IngredientService);
  searchService: SearchService = inject(SearchService);
  breakpointObserver: BreakpointObserver = inject(BreakpointObserver);
  cdr: ChangeDetectorRef = inject(ChangeDetectorRef);

  results: SearchResponseDto | null = null;
  isMobile: boolean = false;
  
  private destroy$: Subject<void> = new Subject<void>();

  @ViewChild('sidenav') sidenav!: MatSidenav;

  constructor() {
    this.breakpointObserver.observe([Breakpoints.Handset])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.matches;
      });
  }

  ngOnInit(): void {
    this.ingredientService.selectedIngredients$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((ingredients: IngredientDto[]): Observable<SearchResponseDto | null> => {
          const request: SearchRequestDto = new SearchRequestDto({
            ingredientIds: ingredients.map(i => i.id!),
            allowSubstitutions: true
          });
          return this.searchService.search(request).pipe(
            catchError(error => {
              console.error('Search failed', error);
              return of(null);
            })
          );
        })
      )
      .subscribe((response: SearchResponseDto | null) => {
        this.results = response;
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

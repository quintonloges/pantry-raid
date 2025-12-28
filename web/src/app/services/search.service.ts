import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SearchClient, SearchRequestDto, SearchResponseDto } from './pantry-raid-api';

@Injectable({
  providedIn: 'root'
})
export class SearchService {
  constructor(private searchClient: SearchClient) {}

  search(request: SearchRequestDto): Observable<SearchResponseDto> {
    return this.searchClient.search(request);
  }
}


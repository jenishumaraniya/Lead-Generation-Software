import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { AdminApiService } from '../../../app/core/services/admin-api.services';

@Component({ 
  selector: 'app-visitor-list', 
  standalone: true,
  templateUrl: './visitor-list.component.html', 
  imports: [CommonModule, DatePipe]
}) 

export class VisitorListComponent 

  implements OnInit { 

 

  visitors: any[] = []; 

 

  constructor( 

    private adminApi: AdminApiService 

  ) {} 

 

  ngOnInit(): void { 

    this.loadVisitors(); 

  } 

 

  loadVisitors(): void { 

 

    this.adminApi.getVisitors() 

      .subscribe({ 

        next: (data) => { 

          this.visitors = data; 

        }, 

 

        error: (error) => { 

          console.error(error); 

        } 

      }); 

  } 

} 

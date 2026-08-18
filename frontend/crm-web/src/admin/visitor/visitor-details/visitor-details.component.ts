import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { AdminApiService } from '../../../app/core/services/admin-api.services';

@Component({ 
  selector: 'app-visitor-details', 
  standalone: true,
  templateUrl: './visitor-details.component.html', 
  imports: [CommonModule, DatePipe]
}) 

export class VisitorDetailsComponent 

  implements OnInit { 

 

  visitor: any; 

 

  constructor( 

    private route: ActivatedRoute, 

    private adminApi: AdminApiService 

  ) {} 

 

  ngOnInit(): void { 

 

    const anonymousId = 

      this.route.snapshot.paramMap 

        .get('anonymousId'); 

 

    if (anonymousId) { 

      this.adminApi 

        .getVisitorDetails(anonymousId) 

        .subscribe({ 

          next: (data) => { 

            this.visitor = data; 

          } 

        }); 

    } 

  } 

} 

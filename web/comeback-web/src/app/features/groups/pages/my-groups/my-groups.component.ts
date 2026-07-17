import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { GroupService } from '../../services/group.service';
import { GroupSummary } from '../../models/group.models';
import { CreateGroupDialogComponent } from '../../components/create-group-dialog/create-group-dialog.component';

@Component({
  selector: 'app-my-groups',
  imports: [
    MatTooltipModule,MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './my-groups.component.html',
  styleUrl: './my-groups.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyGroupsComponent implements OnInit {
  private readonly groupService = inject(GroupService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly groups = signal<GroupSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);

  ngOnInit() {
    this.load();
  }

  openCreate() {
    const ref = this.dialog.open(CreateGroupDialogComponent, { width: '360px' });
    ref.afterClosed().subscribe((created: GroupSummary | undefined) => {
      if (created) this.groups.update(list => [created, ...list]);
    });
  }

  goToGroup(id: string) {
    this.router.navigate(['/groups', id]);
  }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.groupService.getMyGroups().subscribe({
      next: (g) => { this.groups.set(g); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }
}

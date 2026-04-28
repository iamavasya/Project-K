import { Component, inject, OnInit } from '@angular/core';
import { UserService } from '../../services/user.service';
import { UserDto } from '../../models/userDto';
import { TableModule } from 'primeng/table';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { SelectModule } from "primeng/select";
import { ButtonModule } from 'primeng/button';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [TableModule, InputTextModule, IconFieldModule, InputIconModule, FormsModule, CommonModule, SelectModule, ButtonModule, ToastModule, ConfirmDialogModule],
  providers: [MessageService, ConfirmationService],
  templateUrl: './users-list.html',
  styleUrl: './users-list.css'
})
export class UsersListComponent implements OnInit {
  users: UserDto[] = [];
  clonedUsers: Record<string, UserDto> = {};
  expandedRows: Record<string, boolean> = {};
  private readonly userService = inject(UserService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  roles = [
    { label: 'Admin', value: 0 },
    { label: 'Manager', value: 1 },
    { label: 'Mentor', value: 2 },
    { label: 'User', value: 3 }
  ];

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users.sort((a, b) => (a.kurinNumber ?? 999) - (b.kurinNumber ?? 999));

        this.users.forEach(user => {
          if (user.kurinNumber !== null) {
            this.expandedRows[user.kurinNumber.toString()] = false;
          } else {
            this.expandedRows['null'] = false;
          }
        });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load users' })
    });
  }

  toggleGroup(kurinNumber: number | null) {
    const key = kurinNumber?.toString() ?? 'null';
    this.expandedRows[key] = !this.expandedRows[key];
  }

  deleteUser(user: UserDto) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete user ${user.firstName} ${user.lastName}? This action cannot be undone.`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.userService.deleteUser(user.userId).subscribe({
          next: () => {
            this.users = this.users.filter(u => u.userId !== user.userId);
            this.messageService.add({ severity: 'success', summary: 'Success', detail: 'User deleted successfully' });
          },
          error: (err) => {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to delete user' });
          }
        });
      }
    });
  }

  onRowEditInit(user: UserDto) {
    this.clonedUsers[user.userId] = { ...user };
  }

  onRowEditSave(user: UserDto) {
    // Determine new role as number based on string label (assuming select mutates user.role to be a string or number temporarily, we need to map it)
    const newRoleValue = this.roles.find(r => r.label === user.role)?.value;
    
    if (newRoleValue === undefined) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Invalid role selected.' });
      this.users[this.users.findIndex(u => u.userId === user.userId)] = this.clonedUsers[user.userId];
      delete this.clonedUsers[user.userId];
      return;
    }

    this.userService.changeUserRole(user.userId, newRoleValue).subscribe({
      next: () => {
        delete this.clonedUsers[user.userId];
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Role updated successfully' });
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to update role' });
        this.users[this.users.findIndex(u => u.userId === user.userId)] = this.clonedUsers[user.userId];
        delete this.clonedUsers[user.userId];
      }
    });
  }

  onRowEditCancel(user: UserDto, index: number) {
    this.users[index] = this.clonedUsers[user.userId];
    delete this.clonedUsers[user.userId];
  }
}

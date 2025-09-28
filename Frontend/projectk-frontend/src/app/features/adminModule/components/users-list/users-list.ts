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


@Component({
  selector: 'app-users-list',
  imports: [TableModule, InputTextModule, IconFieldModule, InputIconModule, FormsModule, CommonModule, SelectModule, ButtonModule],
  templateUrl: './users-list.html',
  styleUrl: './users-list.css'
})
export class UsersListComponent implements OnInit {
  users: UserDto[] = [];
  clonedUsers: { [s: string]: UserDto } = {};
  private readonly userService = inject(UserService);

  // TODO: Продовжити роботу над компонентом юзерів
  // - Додати таблицю з юзерами (PrimeNG Table)
  // - Додати можливість фільтрації, сортування, пагінації
  // - Додати можливість редагування ролі
  // - Додати можливість видалення юзера
  // - Додати можливість створення нового юзера
  // - Додати можливість скидання пароля юзера
  // - Додати можливість призначення юзера до куреня
  // - Додати можливість пошуку юзера по email, firstName, lastName
  // - Додати валідацію на стороні клієнта
  // - Додати повідомлення про успішні/неуспішні дії
  // - Додати лоадер під час завантаження даних
  // - Додати обробку помилок
  ngOnInit() {
    this.userService.getAllUsers().subscribe(users => {
      this.users = users;
    });
  }

  onRowEditInit(user: UserDto) {
    this.clonedUsers[user.userId as string] = { ...user };
  }

  onRowEditSave(user: UserDto) {
    delete this.clonedUsers[user.userId as string];
    // this.userService.updateUser(user).subscribe({
    //   next: (updatedUser: UserDto) => {
    //     const index = this.users.findIndex(u => u.userId === updatedUser.userId);
    //     if (index !== -1) {
    //       this.users[index] = updatedUser;
    //     }
    //   }
    // });
  }

  onRowEditCancel(user: UserDto, index: number) {
    this.users[index] = this.clonedUsers[user.userId as string];
    delete this.clonedUsers[user.userId as string];
  }
}

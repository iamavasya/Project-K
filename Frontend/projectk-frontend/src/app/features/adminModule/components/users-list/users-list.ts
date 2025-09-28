import { Component, inject, OnInit } from '@angular/core';
import { UserService } from '../../services/user.service';
import { UserDto } from '../../models/userDto';

@Component({
  selector: 'app-users-list',
  imports: [],
  templateUrl: './users-list.html',
  styleUrl: './users-list.css'
})
export class UsersListComponent implements OnInit {
  users: UserDto[] = [];
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
      console.log(users);
    });
  }
}

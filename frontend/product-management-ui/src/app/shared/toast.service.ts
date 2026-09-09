import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly messagesSubject = new BehaviorSubject<ToastMessage[]>([]);
  readonly messages$ = this.messagesSubject.asObservable();

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  dismiss(id: number): void {
    this.messagesSubject.next(this.messagesSubject.value.filter(item => item.id !== id));
  }

  private show(type: ToastType, message: string): void {
    const toast = { id: this.nextId++, type, message };
    this.messagesSubject.next([...this.messagesSubject.value, toast]);
    window.setTimeout(() => this.dismiss(toast.id), 3500);
  }
}

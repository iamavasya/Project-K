import { TestBed } from '@angular/core/testing';
import { MessageService } from '@openng/optimus-ui/api';
import { of, throwError } from 'rxjs';
import { PhoneAppInstallCode, PhoneAppService } from '../../services/phone-app-service/phone-app.service';
import { InstallOnPhoneDialogComponent } from './install-on-phone-dialog';

describe('InstallOnPhoneDialogComponent', () => {
  const installCode: PhoneAppInstallCode = { url: 'https://lileyka.example/m/install', qrCode: 'data:image/png;base64,AAAA' };
  let messages: jasmine.SpyObj<MessageService>;
  let phoneApp: { installCode$: unknown };

  function setup(): void {
    TestBed.configureTestingModule({
      imports: [InstallOnPhoneDialogComponent],
      providers: [
        { provide: MessageService, useValue: messages },
        { provide: PhoneAppService, useValue: phoneApp }
      ]
    });
  }

  beforeEach(() => {
    messages = jasmine.createSpyObj<MessageService>('MessageService', ['add']);
    phoneApp = { installCode$: of(installCode) };
  });

  it('asks the server for the code only once the dialog opens', () => {
    setup();
    const fixture = TestBed.createComponent(InstallOnPhoneDialogComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.code()).toBeNull();

    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    expect(fixture.componentInstance.code()).toEqual(installCode);
  });

  it('says so when the code cannot be read', () => {
    phoneApp = { installCode$: throwError(() => new Error('offline')) };
    setup();
    const fixture = TestBed.createComponent(InstallOnPhoneDialogComponent);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    expect(fixture.componentInstance.failed()).toBeTrue();
  });

  it('shows the address itself when the clipboard refuses', async () => {
    setup();
    spyOn(navigator.clipboard, 'writeText').and.rejectWith(new Error('denied'));
    const component = TestBed.createComponent(InstallOnPhoneDialogComponent).componentInstance;

    await component.copyLink(installCode.url);

    expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'warn', detail: installCode.url }));
  });
});

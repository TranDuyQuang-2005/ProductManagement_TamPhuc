import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { ProductFormComponent } from './product-form.component';
import { ProductService } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../shared/toast.service';

describe('ProductFormComponent', () => {
  let fixture: ComponentFixture<ProductFormComponent>;
  let component: ProductFormComponent;
  let productService: jasmine.SpyObj<ProductService>;

  beforeEach(async () => {
    productService = jasmine.createSpyObj<ProductService>('ProductService', ['create', 'update', 'getById']);
    productService.create.and.returnValue(of({ success: true, message: 'OK', data: productResponse() }));

    await TestBed.configureTestingModule({
      imports: [ProductFormComponent],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: productService },
        {
          provide: CategoryService,
          useValue: jasmine.createSpyObj<CategoryService>('CategoryService', ['getOptions', 'getProductCodePreview'])
        },
        { provide: AuthService, useValue: { isStaff: false, isAdmin: true } },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']) },
        {
          provide: Router,
          useValue: jasmine.createSpyObj<Router>('Router', ['navigate'])
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductFormComponent);
    component = fixture.componentInstance;
  });

  it('rejects empty product name and does not call API', () => {
    setValidForm();
    component.form.controls.productName.setValue('');

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(productService.create).not.toHaveBeenCalled();
  });

  it('rejects whitespace product name and does not call API', () => {
    setValidForm();
    component.form.controls.productName.setValue('   ');

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(productService.create).not.toHaveBeenCalled();
  });

  it('rejects negative price and does not call API', () => {
    setValidForm();
    component.form.controls.price.setValue(-0.01);

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(productService.create).not.toHaveBeenCalled();
  });

  it('accepts price equal to zero without stock payload', () => {
    setValidForm();
    component.form.controls.price.setValue(0);

    component.submit();

    expect(productService.create).toHaveBeenCalledOnceWith(jasmine.objectContaining({ price: 0 }));
    expect(productService.create.calls.mostRecent().args[0]).not.toEqual(jasmine.objectContaining({ stockQuantity: jasmine.anything() }));
  });

  it('rejects values with more than 2 decimals and does not call API', () => {
    setValidForm();
    component.form.controls.price.setValue(1.001);

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(productService.create).not.toHaveBeenCalled();
  });

  it('rejects missing category and does not call API', () => {
    setValidForm();
    component.form.controls.categoryId.setValue(null);

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(productService.create).not.toHaveBeenCalled();
  });

  function setValidForm(): void {
    component.form.patchValue({
      productName: 'Valid product',
      categoryId: 1,
      unit: 'kg',
      price: 1,
      description: '',
      isActive: true
    });
  }
});

function productResponse(): any {
  return {
    id: 1,
    productCode: 'PR000001',
    productName: 'Valid product',
    categoryId: 1,
    unit: 'kg',
    price: 1,
    stockQuantity: 0,
    isActive: true
  };
}

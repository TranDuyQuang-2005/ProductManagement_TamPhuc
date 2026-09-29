import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProductListComponent } from './product-list.component';
import { ProductService } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../shared/toast.service';
import { Product } from '../../models/product.model';

describe('ProductListComponent', () => {
  let fixture: ComponentFixture<ProductListComponent>;
  let component: ProductListComponent;
  let productService: jasmine.SpyObj<ProductService>;

  beforeEach(async () => {
    productService = jasmine.createSpyObj<ProductService>('ProductService', [
      'search',
      'delete',
      'searchTrash',
      'restore',
      'deletePermanent',
      'history',
      'stockIn'
    ]);
    productService.search.and.returnValue(of(paged([])));
    productService.delete.and.returnValue(of({ success: true, message: 'OK', data: {} }));
    productService.searchTrash.and.returnValue(of(paged([])));
    productService.restore.and.returnValue(of({ success: true, message: 'OK', data: product() }));
    productService.deletePermanent.and.returnValue(of({ success: true, message: 'OK', data: {} }));
    productService.stockIn.and.returnValue(of({
      success: true,
      message: 'OK',
      data: {
        id: 1,
        productId: 1,
        movementType: 'STOCK_IN',
        quantityChange: 5,
        quantityBefore: 1,
        quantityAfter: 6,
        referenceCode: null,
        note: null,
        createdByUserId: null,
        username: null,
        createdAt: '2026-01-01T00:00:00Z'
      }
    }));

    await TestBed.configureTestingModule({
      imports: [ProductListComponent],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: productService },
        { provide: CategoryService, useValue: jasmine.createSpyObj<CategoryService>('CategoryService', ['getOptions']) },
        { provide: AuthService, useValue: { isAdmin: true, isStaff: false } },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;
  });

  it('search calls product service', () => {
    component.filterForm.patchValue({ keyword: 'PR000001' });

    component.search();

    expect(productService.search).toHaveBeenCalledWith(jasmine.objectContaining({ keyword: 'PR000001', page: 1 }));
  });

  it('soft delete refreshes product list', () => {
    const item = product();
    component.products = [item];

    component.deleteProduct(item);
    component.confirmDeleteProduct();

    expect(productService.delete).toHaveBeenCalledOnceWith(item.id);
    expect(productService.search).toHaveBeenCalled();
  });

  it('openTrash loads trash products', () => {
    component.openTrash();

    expect(productService.searchTrash).toHaveBeenCalled();
  });

  it('restore refreshes trash and product list', () => {
    const item = product({ isDeleted: true });
    component.trashProducts = [item];

    component.restoreProduct(item);

    expect(productService.restore).toHaveBeenCalledOnceWith(item.id);
    expect(productService.searchTrash).toHaveBeenCalled();
    expect(productService.search).toHaveBeenCalled();
  });

  it('permanent delete refreshes trash', () => {
    const item = product({ isDeleted: true });
    component.trashProducts = [item];

    component.deletePermanentProduct(item);
    component.confirmPermanentDeleteProduct();

    expect(productService.deletePermanent).toHaveBeenCalledOnceWith(item.id);
    expect(productService.searchTrash).toHaveBeenCalled();
  });

  it('stock-in submits quantity and refreshes product list', () => {
    const item = product();

    component.openStockIn(item);
    component.stockInForm.patchValue({ quantity: 5, referenceCode: 'pn001', note: 'test' });
    component.submitStockIn();

    expect(productService.stockIn).toHaveBeenCalledOnceWith({
      productId: item.id,
      quantity: 5,
      referenceCode: 'pn001',
      note: 'test'
    });
    expect(productService.search).toHaveBeenCalled();
  });
});

function paged(items: Product[]) {
  return {
    success: true,
    message: 'OK',
    data: {
      items,
      page: 1,
      pageSize: 10,
      totalItems: items.length,
      totalPages: items.length ? 1 : 0
    }
  };
}

function product(overrides: Partial<Product> = {}): Product {
  return {
    id: 1,
    productCode: 'PR000001',
    productName: 'Product',
    categoryId: 1,
    categoryCode: 'CAT',
    categoryName: 'Category',
    categoryIsActive: true,
    unit: 'kg',
    price: 1,
    stockQuantity: 1,
    stockStatus: 'Còn hàng',
    description: null,
    isActive: true,
    createdByUserId: null,
    createdByUsername: null,
    lastModifiedByUserId: null,
    lastModifiedByUsername: null,
    isAdminProtected: false,
    isDeleted: false,
    deletedAt: null,
    deletedByUserId: null,
    deletedByUsername: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    ...overrides
  };
}

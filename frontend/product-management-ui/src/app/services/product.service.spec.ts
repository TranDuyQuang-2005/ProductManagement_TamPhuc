import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { ProductCreatePayload, ProductSearchParams, ProductUpdatePayload, StockInPayload } from '../models/product.model';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;
  const baseUrl = `${environment.apiBaseUrl}/products`;
  const inventoryUrl = `${environment.apiBaseUrl}/inventory`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ProductService]
    });
    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('search sends POST /products/search with body', () => {
    const params: ProductSearchParams = {
      keyword: 'PR000001',
      page: 1,
      pageSize: 10,
      sortBy: 'stockQuantity',
      sortDirection: 'asc',
      stockStatus: 'InStock',
      minPrice: 1,
      maxPrice: 100,
      isActive: true
    };

    service.search(params).subscribe();

    const request = http.expectOne(`${baseUrl}/search`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(params);
    request.flush({ success: true, message: 'OK', data: { items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 } });
  });

  it('create sends POST /products/create without stock payload', () => {
    const payload: ProductCreatePayload = validCreatePayload();

    service.create(payload).subscribe();

    const request = http.expectOne(`${baseUrl}/create`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    expect(request.request.body.stockQuantity).toBeUndefined();
    request.flush({ success: true, message: 'OK', data: {} });
  });

  it('update sends POST /products/update with id in body', () => {
    const payload: ProductUpdatePayload = validUpdatePayload();

    service.update(7, payload).subscribe();

    const request = http.expectOne(`${baseUrl}/update`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ id: 7, ...payload });
    expect(request.request.body.stockQuantity).toBeUndefined();
    request.flush({ success: true, message: 'OK', data: {} });
  });

  it('delete sends POST /products/delete', () => {
    service.delete(7).subscribe();

    const request = http.expectOne(`${baseUrl}/delete`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ id: 7 });
    request.flush({ success: true, message: 'OK', data: {} });
  });

  it('searchTrash sends POST /products/trash/search', () => {
    const params: ProductSearchParams = { page: 1, pageSize: 10, sortBy: 'createdAt', sortDirection: 'desc' };
    service.searchTrash(params).subscribe();

    const request = http.expectOne(`${baseUrl}/trash/search`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(params);
    request.flush({ success: true, message: 'OK', data: { items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 } });
  });

  it('restore sends POST /products/restore', () => {
    service.restore(7).subscribe();

    const request = http.expectOne(`${baseUrl}/restore`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ id: 7 });
    request.flush({ success: true, message: 'OK', data: {} });
  });

  it('deletePermanent sends POST /products/delete-permanent', () => {
    service.deletePermanent(7).subscribe();

    const request = http.expectOne(`${baseUrl}/delete-permanent`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ id: 7 });
    request.flush({ success: true, message: 'OK', data: {} });
  });

  it('stockIn sends POST /inventory/stock-in', () => {
    const payload: StockInPayload = { productId: 7, quantity: 2.5, referenceCode: 'PN001', note: 'Nhap hang' };

    service.stockIn(payload).subscribe();

    const request = http.expectOne(`${inventoryUrl}/stock-in`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({ success: true, message: 'OK', data: {} });
  });
});

function validCreatePayload(): ProductCreatePayload {
  return {
    productName: 'Product',
    categoryId: 1,
    unit: 'kg',
    price: 1,
    description: null,
    isActive: true
  };
}

function validUpdatePayload(): ProductUpdatePayload {
  return {
    productName: 'Product',
    unit: 'kg',
    price: 1,
    description: null,
    isActive: true
  };
}

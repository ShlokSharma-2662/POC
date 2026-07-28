import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { Modal } from '../../components/ui/Modal';
import { Pagination } from '../../components/ui/Pagination';
import { adminApi } from './admin-api';
import {
  EmptyState,
  ErrorState,
  PageHero,
  StatusBadge,
  TableLoader,
} from './admin-components';
import styles from './Admin.module.scss';
import type { AdminProduct, ProductDraft } from './types';
import { useDebouncedValue } from './use-debounced-value';
import {
  datedFilename,
  displayError,
  downloadCsv,
  formatCurrency,
  formatNumber,
  totalPages,
} from './utils';

type ProductEditor =
  | { mode: 'create' }
  | { mode: 'edit'; product: AdminProduct };

interface ProductSave {
  editor: ProductEditor;
  draft: ProductDraft;
  image?: File;
}

const emptyDraft: ProductDraft = {
  name: '',
  description: '',
  price: 0,
  stock: 0,
  imageUrl: '',
  categoryId: 0,
};

export function AdminProductsPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [deletedFilter, setDeletedFilter] = useState('all');
  const [categoryId, setCategoryId] = useState(0);
  const [editor, setEditor] = useState<ProductEditor | null>(null);
  const [draft, setDraft] = useState<ProductDraft>(emptyDraft);
  const [selectedFile, setSelectedFile] = useState<File | undefined>();
  const [filePreview, setFilePreview] = useState('');
  const [confirmProduct, setConfirmProduct] = useState<AdminProduct | null>(null);
  const debouncedSearch = useDebouncedValue(search.trim());

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: adminApi.getCategories,
    staleTime: 5 * 60 * 1000,
  });

  const productsQuery = useQuery({
    queryKey: [
      'admin',
      'products',
      page,
      pageSize,
      debouncedSearch,
      deletedFilter,
      categoryId,
    ],
    queryFn: () =>
      adminApi.getProducts({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        isDeleted:
          deletedFilter === 'all' ? undefined : deletedFilter === 'deleted',
        categoryId: categoryId || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  useEffect(
    () => () => {
      if (filePreview) URL.revokeObjectURL(filePreview);
    },
    [filePreview],
  );

  const saveMutation = useMutation<number | boolean, Error, ProductSave>({
    mutationFn: ({ editor: pendingEditor, draft: values, image }: ProductSave) =>
      pendingEditor.mode === 'edit'
        ? adminApi.updateProduct(pendingEditor.product.productId, values, image)
        : adminApi.createProduct(values, image),
    onSuccess: async (_data, variables) => {
      toast.success(
        variables.editor.mode === 'edit'
          ? 'Product updated successfully.'
          : 'Product created successfully.',
      );
      closeEditor();
      await queryClient.invalidateQueries({ queryKey: ['admin', 'products'] });
    },
    onError: (error) =>
      toast.error(displayError(error, 'The product could not be saved.')),
  });

  const deleteMutation = useMutation({
    mutationFn: (product: AdminProduct) =>
      product.isDeleted
        ? adminApi.restoreProduct(product.productId)
        : adminApi.deleteProduct(product.productId),
    onSuccess: async (_data, product) => {
      toast.success(
        product.isDeleted
          ? 'Product restored successfully.'
          : 'Product deleted successfully.',
      );
      setConfirmProduct(null);
      await queryClient.invalidateQueries({ queryKey: ['admin', 'products'] });
    },
    onError: (error) =>
      toast.error(displayError(error, 'The product could not be updated.')),
  });

  const categories = categoriesQuery.data ?? [];
  const products = productsQuery.data?.items ?? [];
  const count = productsQuery.data?.totalCount ?? 0;

  function startCreate() {
    setDraft({
      ...emptyDraft,
      categoryId: categories[0]?.id ?? 0,
    });
    setSelectedFile(undefined);
    setFilePreview('');
    setEditor({ mode: 'create' });
  }

  function startEdit(product: AdminProduct) {
    setDraft({
      name: product.name,
      description: product.description,
      price: product.price,
      stock: product.stock,
      imageUrl: product.imageUrl ?? '',
      categoryId: product.categoryId,
    });
    setSelectedFile(undefined);
    setFilePreview('');
    setEditor({ mode: 'edit', product });
  }

  function closeEditor() {
    setEditor(null);
    setDraft(emptyDraft);
    setSelectedFile(undefined);
    setFilePreview('');
  }

  function selectImage(file?: File) {
    setSelectedFile(file);
    setFilePreview(file ? URL.createObjectURL(file) : '');
  }

  function saveProduct() {
    if (!editor) return;
    if (!draft.name.trim()) {
      toast.error('Enter a product name.');
      return;
    }
    if (!draft.description.trim()) {
      toast.error('Enter a product description.');
      return;
    }
    if (draft.price < 0 || draft.stock < 0) {
      toast.error('Price and stock cannot be negative.');
      return;
    }
    if (draft.categoryId <= 0) {
      toast.error('Select a category.');
      return;
    }
    saveMutation.mutate({
      editor,
      draft: {
        ...draft,
        name: draft.name.trim(),
        description: draft.description.trim(),
        imageUrl: draft.imageUrl.trim(),
      },
      image: selectedFile,
    });
  }

  function clearFilters() {
    setSearch('');
    setDeletedFilter('all');
    setCategoryId(0);
    setPage(1);
  }

  function categoryName(product: AdminProduct) {
    return (
      product.categoryName ||
      categories.find((category) => category.id === product.categoryId)?.name ||
      'Uncategorised'
    );
  }

  function exportProducts() {
    if (!products.length) return;
    downloadCsv(datedFilename('products_export'), products, [
      { label: 'Product ID', value: (product) => product.productId },
      { label: 'Name', value: (product) => product.name },
      { label: 'Description', value: (product) => product.description },
      { label: 'Price (INR)', value: (product) => product.price.toFixed(2) },
      { label: 'Stock', value: (product) => product.stock },
      { label: 'Category', value: categoryName },
      { label: 'Image URL', value: (product) => product.imageUrl ?? '' },
      {
        label: 'Status',
        value: (product) => (product.isDeleted ? 'Deleted' : 'Active'),
      },
    ]);
    toast.success(`Exported ${products.length} products.`);
  }

  return (
    <main className={styles.page}>
      <PageHero
        title="Manage products"
        description="Create, update, archive, and restore products in the catalogue."
        actions={
          <>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={exportProducts}
              disabled={!products.length}
            >
              Export current page
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={startCreate}
            >
              Add product
            </button>
          </>
        }
      />

      <section className={styles.panel} aria-label="Product filters">
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <label htmlFor="products-search">Search products</label>
            <input
              className={styles.input}
              id="products-search"
              type="search"
              value={search}
              placeholder="Name or description"
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className={styles.filterField}>
            <label htmlFor="products-status">Status</label>
            <select
              className={styles.select}
              id="products-status"
              value={deletedFilter}
              onChange={(event) => {
                setDeletedFilter(event.target.value);
                setPage(1);
              }}
            >
              <option value="all">All statuses</option>
              <option value="active">Active</option>
              <option value="deleted">Deleted</option>
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="products-category">Category</label>
            <select
              className={styles.select}
              id="products-category"
              value={categoryId}
              onChange={(event) => {
                setCategoryId(Number(event.target.value));
                setPage(1);
              }}
              disabled={categoriesQuery.isPending}
            >
              <option value={0}>All categories</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="products-page-size">Rows per page</label>
            <select
              className={styles.select}
              id="products-page-size"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPage(1);
              }}
            >
              {[10, 25, 50].map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </div>
          <div className={styles.filterActions}>
            <button
              className={styles.ghostButton}
              type="button"
              onClick={clearFilters}
              disabled={!search && deletedFilter === 'all' && !categoryId}
            >
              Clear
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={() => void productsQuery.refetch()}
              disabled={productsQuery.isFetching}
            >
              Refresh
            </button>
          </div>
        </div>
      </section>

      {categoriesQuery.isError ? (
        <div className={`${styles.message} ${styles.errorMessage}`} role="alert">
          Categories could not be loaded. Product editing is unavailable until
          they are refreshed.
        </div>
      ) : null}

      <section className={`${styles.panel} ${styles.tablePanel}`}>
        {productsQuery.isPending ? (
          <TableLoader label="Loading products…" />
        ) : productsQuery.isError ? (
          <ErrorState
            message={displayError(
              productsQuery.error,
              'Products could not be loaded.',
            )}
            onRetry={() => void productsQuery.refetch()}
          />
        ) : products.length === 0 ? (
          <EmptyState
            title="No products found"
            description="Try changing the filters or add a new product."
            action={
              <button
                className={styles.primaryButton}
                type="button"
                onClick={startCreate}
              >
                Add product
              </button>
            }
          />
        ) : (
          <>
            <div className={styles.tableScroll}>
              <table className={styles.table}>
                <caption className={styles.srOnly}>
                  Products, inventory, and catalogue status
                </caption>
                <thead>
                  <tr>
                    <th scope="col">ID</th>
                    <th scope="col">Image</th>
                    <th scope="col">Name</th>
                    <th scope="col">Category</th>
                    <th className={styles.numberCell} scope="col">
                      Price
                    </th>
                    <th className={styles.numberCell} scope="col">
                      Stock
                    </th>
                    <th scope="col">Status</th>
                    <th className={styles.actionsCell} scope="col">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {products.map((product) => (
                    <tr key={product.productId}>
                      <td>{product.productId}</td>
                      <td>
                        {product.imageUrl ? (
                          <img
                            className={styles.thumbnail}
                            src={product.imageUrl}
                            alt=""
                            loading="lazy"
                          />
                        ) : (
                          <span className={styles.toolbarText}>No image</span>
                        )}
                      </td>
                      <td>{product.name}</td>
                      <td>{categoryName(product)}</td>
                      <td className={styles.numberCell}>
                        {formatCurrency(product.price)}
                      </td>
                      <td className={styles.numberCell}>
                        {formatNumber(product.stock)}
                      </td>
                      <td>
                        <StatusBadge
                          value={product.isDeleted ? 'Deleted' : 'Active'}
                        />
                      </td>
                      <td className={styles.actionsCell}>
                        <div className={styles.inlineActions}>
                          <button
                            className={`${styles.secondaryButton} ${styles.compactButton}`}
                            type="button"
                            disabled={product.isDeleted || categoriesQuery.isError}
                            onClick={() => startEdit(product)}
                          >
                            Edit
                          </button>
                          <button
                            className={`${styles.ghostButton} ${styles.compactButton}`}
                            type="button"
                            onClick={() => setConfirmProduct(product)}
                          >
                            {product.isDeleted ? 'Restore' : 'Delete'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className={styles.tableFooter}>
              <span>
                Showing {(page - 1) * pageSize + 1}–
                {Math.min(page * pageSize, count)} of {formatNumber(count)}
              </span>
              <Pagination
                page={page}
                totalPages={totalPages(count, pageSize)}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </section>

      <Modal
        open={editor !== null}
        title={editor?.mode === 'edit' ? 'Edit product' : 'Add product'}
        size="lg"
        onClose={closeEditor}
        footer={
          <>
            <button
              className={styles.ghostButton}
              type="button"
              onClick={closeEditor}
              disabled={saveMutation.isPending}
            >
              Cancel
            </button>
            <button
              className={styles.primaryButton}
              type="button"
              onClick={saveProduct}
              disabled={saveMutation.isPending || categoriesQuery.isError}
            >
              {saveMutation.isPending ? 'Saving…' : 'Save product'}
            </button>
          </>
        }
      >
        <form
          className={styles.formGrid}
          onSubmit={(event) => {
            event.preventDefault();
            saveProduct();
          }}
        >
          <div className={`${styles.formField} ${styles.wideField}`}>
            <label htmlFor="product-name">Name</label>
            <input
              className={styles.input}
              id="product-name"
              required
              value={draft.name}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  name: event.target.value,
                }))
              }
            />
          </div>
          <div className={`${styles.formField} ${styles.wideField}`}>
            <label htmlFor="product-description">Description</label>
            <textarea
              className={styles.textarea}
              id="product-description"
              required
              value={draft.description}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  description: event.target.value,
                }))
              }
            />
          </div>
          <div className={styles.formField}>
            <label htmlFor="product-price">Price (INR)</label>
            <input
              className={styles.input}
              id="product-price"
              type="number"
              min={0}
              step="0.01"
              required
              value={draft.price}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  price: Number(event.target.value),
                }))
              }
            />
          </div>
          <div className={styles.formField}>
            <label htmlFor="product-stock">Stock</label>
            <input
              className={styles.input}
              id="product-stock"
              type="number"
              min={0}
              step={1}
              required
              value={draft.stock}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  stock: Number(event.target.value),
                }))
              }
            />
          </div>
          <div className={styles.formField}>
            <label htmlFor="product-category">Category</label>
            <select
              className={styles.select}
              id="product-category"
              required
              value={draft.categoryId}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  categoryId: Number(event.target.value),
                }))
              }
            >
              <option value={0} disabled>
                Select a category
              </option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>
          <div className={styles.formField}>
            <label htmlFor="product-image-file">Upload image</label>
            <input
              className={styles.input}
              id="product-image-file"
              type="file"
              accept="image/*"
              onChange={(event) => selectImage(event.target.files?.[0])}
            />
          </div>
          <div className={`${styles.formField} ${styles.wideField}`}>
            <label htmlFor="product-image-url">Image URL</label>
            <input
              className={styles.input}
              id="product-image-url"
              type="text"
              value={draft.imageUrl}
              placeholder="/images/products/example.jpg"
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  imageUrl: event.target.value,
                }))
              }
            />
          </div>
          {filePreview || draft.imageUrl ? (
            <div className={`${styles.fileRow} ${styles.wideField}`}>
              <img
                className={styles.preview}
                src={filePreview || draft.imageUrl}
                alt="Product image preview"
              />
              <p className={styles.toolbarText}>
                {selectedFile
                  ? `${selectedFile.name} (${Math.ceil(selectedFile.size / 1024)} KB)`
                  : 'Current image'}
              </p>
            </div>
          ) : null}
          <button className={styles.srOnly} type="submit">
            Save
          </button>
        </form>
      </Modal>

      <Modal
        open={confirmProduct !== null}
        title={confirmProduct?.isDeleted ? 'Restore product' : 'Delete product'}
        size="sm"
        onClose={() => setConfirmProduct(null)}
        footer={
          <>
            <button
              className={styles.ghostButton}
              type="button"
              disabled={deleteMutation.isPending}
              onClick={() => setConfirmProduct(null)}
            >
              Cancel
            </button>
            <button
              className={
                confirmProduct?.isDeleted
                  ? styles.successButton
                  : styles.dangerButton
              }
              type="button"
              disabled={!confirmProduct || deleteMutation.isPending}
              onClick={() => {
                if (confirmProduct) deleteMutation.mutate(confirmProduct);
              }}
            >
              {deleteMutation.isPending
                ? 'Saving…'
                : confirmProduct?.isDeleted
                  ? 'Restore'
                  : 'Delete'}
            </button>
          </>
        }
      >
        <p>
          {confirmProduct?.isDeleted
            ? 'Restore this product to the active catalogue?'
            : 'Archive this product? Existing order history will be preserved.'}
        </p>
        <p>
          <strong>{confirmProduct?.name}</strong>
        </p>
      </Modal>
    </main>
  );
}

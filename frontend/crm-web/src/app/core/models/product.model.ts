// export interface Product {
//   productId: number;
//   name: string;
//   description: string;
//   pricing: number;
//   features: string[];
//   specifications: string[];
//   status: string;
// }

export interface Product {
  productId: number;
  name: string;
  description: string;
  pricing: number;
  features: string[];
  specifications: string[];
  status: string;
  categoryId?: number;      // optional
  categoryName?: string;    // optional
}
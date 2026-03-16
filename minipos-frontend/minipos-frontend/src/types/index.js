/**
 * @fileoverview Application type definitions
 * Shared across all components and services.
 */

/**
 * @typedef {Object} BasketItem
 * @property {number}  id
 * @property {string}  name
 * @property {string}  barcode
 * @property {number}  price
 * @property {number}  quantity
 * @property {boolean} isFuel
 * @property {string}  emoji
 * @property {string}  category
 */

/**
 * @typedef {Object} Basket
 * @property {string}      basketId
 * @property {string}      cashierId
 * @property {BasketItem[]} items
 * @property {number}      total
 */

/**
 * @typedef {Object} Pump
 * @property {number}  id
 * @property {string}  fuelType
 * @property {string}  status      - IDLE | FUELLING | DONE
 * @property {number}  litresDispensed
 * @property {number}  amount
 */

/**
 * @typedef {Object} Article
 * @property {number}  id
 * @property {string}  name
 * @property {string}  barcode
 * @property {number}  price
 * @property {string}  emoji
 * @property {string}  category
 * @property {boolean} isFuel
 */

/**
 * @typedef {Object} PaymentResult
 * @property {string}  basketId
 * @property {string}  transactionId
 * @property {number}  total
 * @property {string}  paymentMethod
 * @property {string}  status
 * @property {string}  authCode
 */

export {};

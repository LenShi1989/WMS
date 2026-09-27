import { defineStore } from 'pinia'
import { ref } from 'vue'
import { categoryApi, locationApi, materialApi, uomApi, userApi, warehouseApi, zoneApi } from '@/api'
import type { Material, MaterialCategory, Uom, User, Warehouse, WarehouseLocation, Zone } from '@/types'

/**
 * 下拉選單用的主檔快取。入庫、出庫、移庫、盤點等畫面都會用到同一份清單，
 * 集中在這裡可以避免每個畫面各自重複打 API。
 */
export const useMasterStore = defineStore('master', () => {
  const warehouses = ref<Warehouse[]>([])
  const materials = ref<Material[]>([])
  const categories = ref<MaterialCategory[]>([])
  const uoms = ref<Uom[]>([])
  const users = ref<User[]>([])
  const locationsByWarehouse = ref<Record<string, WarehouseLocation[]>>({})
  const zonesByWarehouse = ref<Record<string, Zone[]>>({})

  async function loadWarehouses(force = false) {
    if (warehouses.value.length && !force) return warehouses.value
    warehouses.value = (await warehouseApi.query({ pageSize: 200 })).items
    return warehouses.value
  }

  async function loadMaterials(force = false) {
    if (materials.value.length && !force) return materials.value
    materials.value = (await materialApi.query({ pageSize: 200, isActive: true })).items
    return materials.value
  }

  async function loadCategories(force = false) {
    if (categories.value.length && !force) return categories.value
    categories.value = (await categoryApi.query({ pageSize: 200 })).items
    return categories.value
  }

  async function loadUoms(force = false) {
    if (uoms.value.length && !force) return uoms.value
    uoms.value = (await uomApi.query({ pageSize: 200 })).items
    return uoms.value
  }

  async function loadUsers(force = false) {
    if (users.value.length && !force) return users.value
    users.value = (await userApi.query({ pageSize: 200, isActive: true })).items
    return users.value
  }

  async function loadLocations(warehouseId: string, force = false) {
    if (!warehouseId) return []
    if (locationsByWarehouse.value[warehouseId] && !force) {
      return locationsByWarehouse.value[warehouseId]
    }
    const result = await locationApi.query({ warehouseId, pageSize: 200, isActive: true })
    locationsByWarehouse.value = { ...locationsByWarehouse.value, [warehouseId]: result.items }
    return result.items
  }

  async function loadZones(warehouseId: string, force = false) {
    if (!warehouseId) return []
    if (zonesByWarehouse.value[warehouseId] && !force) {
      return zonesByWarehouse.value[warehouseId]
    }
    const result = await zoneApi.query({ warehouseId, pageSize: 200 })
    zonesByWarehouse.value = { ...zonesByWarehouse.value, [warehouseId]: result.items }
    return result.items
  }

  /** 主檔異動後呼叫，讓下拉選單下次重新抓取。 */
  function invalidate() {
    warehouses.value = []
    materials.value = []
    categories.value = []
    uoms.value = []
    users.value = []
    locationsByWarehouse.value = {}
    zonesByWarehouse.value = {}
  }

  return {
    warehouses, materials, categories, uoms, users, locationsByWarehouse, zonesByWarehouse,
    loadWarehouses, loadMaterials, loadCategories, loadUoms, loadUsers, loadLocations, loadZones,
    invalidate,
  }
})

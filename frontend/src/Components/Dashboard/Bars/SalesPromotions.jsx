import React, { useEffect, useState } from 'react'
import Card from '../Card'
import CardContent from '../CardContent'
import {
  ComposedChart,
  Bar,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer
} from 'recharts'
import formatNumber from '../../../Services/FormatNumberService'
import { getSalesAndPromotionsData } from "../../../Services/SalesAndPromotionsService"

const SalesVsPromotionsChart = ({ filters, showDetails}) => {
  const [data, setData] = useState([])
  const [loading, setLoading] = useState(false)
  const [categories, setCategories] = useState([])

  const fetchData = async () => {
    try {
      setLoading(true)
      const fetchedData = await getSalesAndPromotionsData(filters)
      setData(fetchedData)

      const cats = new Set()
      fetchedData.forEach(d => {
        Object.keys(d).forEach(key => {
          if (key !== 'month' && key !== 'totalSales' && key !== 'NO PROMOTION' && key !=='year') {
            cats.add(key)
          }
        })
      })
      setCategories(Array.from(cats))
    } catch (error) {
      console.log("Error: " + error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchData()
  }, [filters])

  const colors = ['#025848ff', '#ad7f1cff', '#ed5d14ff', '#4640a8ff', '#378e59ff', '#cda963ff']

  return (
    <Card>
      <CardContent>
        <h2 className="chart-title">Sales vs Promotions</h2>
        <ResponsiveContainer width="100%" height={600}>
          <ComposedChart data={data}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey={filters.year ? "month" : "year"} interval={0} angle={20} fontSize={7} />
            <YAxis yAxisId="left" tickFormatter={!showDetails && formatNumber} />
            <YAxis yAxisId="right" orientation="right" unit="%" />
            <Tooltip formatter={(value, name) =>
              name === 'totalSales' ? !showDetails ? formatNumber(value).toLocaleString(): value.toLocaleString() : `${value}%`
            } />
            <Legend />
            <Bar yAxisId="left" dataKey="totalSales" fill="#0088FE" barSize={15} />
            {categories.map((cat, idx) => (
              <Line
                key={cat}
                yAxisId="right"
                type="monotone"
                dataKey={cat}
                stroke={colors[idx % colors.length]}
                strokeWidth={2}
              />
            ))}
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}

export default SalesVsPromotionsChart

import React, { useEffect, useState } from 'react'
import Card from '../Card'
import CardContent from '../CardContent'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts'
import { gerMonthlySalesComparisonWithPreviousYear } from "../../../Services/MonthlySalesService"
import formatNumber from '../../../Services/FormatNumberService'

const MonthlySales = ({ filters, showDetails }) => {
  const [data, setData] = useState([])
  const [loading, setLoading] = useState(false)

  const fetchData = async () => {
    try {
      const fetchedData = await gerMonthlySalesComparisonWithPreviousYear(filters)
      setData(fetchedData)
      console.log("DATA: ")
      console.log(data)
    } catch (error) {
      console.log("Error: " + error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    setLoading(true)
    fetchData()
  }, [filters])

  const hasYear = !!filters.year

  return (
    <Card>
      <CardContent>
        <header>
          <h2 className="chart-title">
            {hasYear
              ? "Monthly Sales Comparison (Current vs Previous Year)"
              : "Monthly Sales Across All Years"}
          </h2>
        </header>

        <ResponsiveContainer width="100%" height={250}>
          {hasYear ? (
            <BarChart
              data={data}
              margin={{ top: 20, right: 30, left: 20, bottom: 40 }}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" interval={0} angle={45} tick={{ fontSize: 10, dy: 15 }} />
              <YAxis tickFormatter={!showDetails && formatNumber} />
              <Tooltip formatter={!showDetails ? (value) => formatNumber(value) : undefined} />
              <Bar dataKey="currentYear" fill="#0088FE" name="Current Year" />
              <Line type="monotone" dataKey="previousYear" stroke="#FF8042" name="Previous Year" />
            </BarChart>
          ) : (
            <BarChart
              data={data}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis
                dataKey="month"
                tick={false}
              />
              <YAxis tickFormatter={!showDetails && formatNumber} />
              <Tooltip
                formatter={!showDetails ? (value) => formatNumber(value) : undefined}
                labelFormatter={(label, payload) => {
                  if (!payload || payload.length === 0) return label
                  const year = payload[0].payload?.year
                  return year ? `${year} - ${label}` : label
                }}
              />
              <Bar dataKey="totalSales" fill="#82ca9d" name="Total Sales" />
            </BarChart>
          )}
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}

export default MonthlySales

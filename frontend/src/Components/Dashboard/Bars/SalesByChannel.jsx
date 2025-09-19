import React, { useEffect, useState } from 'react'
import Card from '../Card'
import CardContent from '../CardContent'
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { getSalesByChannelData } from "../../../Services/SalesByChannelService"
import formatNumber from '../../../Services/FormatNumberService'

const SalesByChannel = ({ filters, showDetails}) => {
    const [data, setData] = useState([])
    const [loading, setLoading] = useState(false)

    const fetchData = async () => {
        try {
            const fetchedData = await getSalesByChannelData(filters)
            setData(fetchedData)
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

    const COLORS = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "#A28CFF", 
                    "#FF6699", "#33CCFF", "#FF9933","#66CC66", "#9966CC"]

    return (
        <Card>
            <CardContent>
                <h2 className="chart-title">Sales by Channel</h2>
                <ResponsiveContainer width="100%" height={250}>
                    <PieChart>
                        <Pie
                            data={data}
                            cx="50%"
                            cy="50%"
                            labelLine={false}
                            label={({ index, x, y }) => {
                                const entry = data[index]
                                return (
                                    <text
                                    x={x}
                                    y={y+5}              
                                    textAnchor="middle"
                                    fill="#000"
                                    fontSize={10}      
                                    >
                                    {`${entry.channel}: ${!showDetails ? formatNumber(entry.totalQuantity) : entry.totalQuantity} (${entry.percentOfTotal}%)`}
                                    </text>
                                )
                            }}
                            outerRadius={100}
                            dataKey="totalQuantity"
                        >
                            {data.map((entry, index) => (
                                <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                            ))}
                        </Pie>
                        <Tooltip formatter={(value, name, props) => [`${!showDetails ? formatNumber(value) : value}`, props.payload.channel]} />
                    </PieChart>
                </ResponsiveContainer>
            </CardContent>
        </Card>
    )
}

export default SalesByChannel

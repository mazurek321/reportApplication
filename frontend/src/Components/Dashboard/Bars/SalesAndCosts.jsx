import React, { useEffect, useState } from 'react'
import { getSalesAndCostsData } from "../../../Services/SalesAndCostsService"
import Card from '../Card';
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import formatNumber from '../../../Services/FormatNumberService';

const SalesAndCosts = ({ filters, showDetails }) => {
    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(false);
    const hasYear = !!filters.year;

    const fetchData = async () => {
        try {
            const fetchedData = await getSalesAndCostsData(filters);
            setData(fetchedData);
        } catch (error) {
            console.log("Error: " + error);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        setLoading(true);
        fetchData();
    }, [filters]);

    return (
        <Card>
            <header>
                <h2 className="chart-title">
                    {hasYear ? "Sales and Costs (Monthly)" : "Sales and Costs (Yearly)"}
                </h2>
            </header>
            <ResponsiveContainer width="100%" height={250}>
                <AreaChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                        dataKey={hasYear ? "month" : "year"} 
                        interval={0} 
                        tick={{ fontSize: 10, fill: "#333", angle: hasYear ? 15 : 0, dy: hasYear ? 15 : 0 }} 
                    />
                    <YAxis 
                        tickFormatter={!showDetails && formatNumber}
                        tick={{ fontSize: 12, fill: "#333" }} 
                    />
                    <Tooltip formatter={!showDetails ? (value) => formatNumber(value) : undefined} />
                    <Area
                        type="monotone"
                        dataKey={hasYear ? "amount_Sold" : "totalSales"}
                        stroke="#0088FE"
                        fill="#0088FE"
                        fillOpacity={0.3}
                        name="Sales"
                    />
                    <Area
                        type="monotone"
                        dataKey={hasYear ? "cost" : "totalCost"}
                        stroke="#FF8042"
                        fill="#FF8042"
                        fillOpacity={0.3}
                        name="Cost"
                    />
                </AreaChart>
            </ResponsiveContainer>
        </Card>
    )
}

export default SalesAndCosts

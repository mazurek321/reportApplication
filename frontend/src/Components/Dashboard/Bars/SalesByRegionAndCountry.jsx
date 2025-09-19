import React, { useEffect, useState } from 'react'
import { getSalesByRegionAndCountry } from "../../../Services/SalesByRegionAndCountryService"
import Card from '../Card';
import CardContent from '../CardContent';
import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import formatNumber from '../../../Services/FormatNumberService';

const COLORS = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "#A28CFF", "#82ca9d", "#ffc658", "#d0ed57"];

const SalesByRegionAndCountry = ({filters, showDetails}) => {
    const [data, setData] = useState([]);
    const [countries, setCountries] = useState([]);

    const fetchData = async () => {
        try {
            const fetchedData = await getSalesByRegionAndCountry(filters);
            setData(fetchedData);
            const allCountries = new Set();
            fetchedData.forEach(region =>
                Object.keys(region).forEach(key => {
                    if (key !== "region") allCountries.add(key);
                })
            );
            setCountries([...allCountries]);
        } catch (error) {
            console.log(error);
        }
    }

    useEffect(() => {
        fetchData();
    }, [filters]);



    return (
        <Card>
            <CardContent>
                <header>
                    <h2 className="chart-title">Sales by Region & Country</h2>
                </header>
                <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={data} layout="vertical" >
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis type="number" tickFormatter={!showDetails && formatNumber}/>
                        <YAxis type="category" dataKey="region"/>
                        <Tooltip formatter={!showDetails ? (value) => formatNumber(value) : undefined} />
                        {countries.map((country, index) => (
                            <Bar key={country} dataKey={country} stackId="a" fill={COLORS[index % COLORS.length]}>
                            </Bar>
                        ))}
                    </BarChart>
                </ResponsiveContainer>
            </CardContent>
        </Card>
    )
}

export default SalesByRegionAndCountry

const formatNumber = (num) => {
  if (!num && num !== 0) return '';

  if (num >= 1_000_000_000) return (num / 1_000_000_000).toFixed(1) + " B";
  if (num >= 1_000_000)     return (num / 1_000_000).toFixed(1) + " M";
  if (num >= 1_000)         return (num / 1_000).toFixed(1) + " K";
  
  return num.toString();
};

export default formatNumber;